import { spawn } from "node:child_process";
import { mkdtemp, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join, sep } from "node:path";
import net from "node:net";

const edgePath = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const appUrl = process.argv[2] ?? "http://localhost:3001/";
const port = await findOpenPort();
const profile = await mkdtemp(join(tmpdir(), "xau-ui-smoke-"));
const screenshotPath = join(tmpdir(), `xau-dashboard-${Date.now()}.png`);
const browser = spawn(edgePath, [
  "--headless=new",
  "--disable-gpu",
  "--no-first-run",
  "--no-default-browser-check",
  `--remote-debugging-port=${port}`,
  `--user-data-dir=${profile}`,
  "about:blank",
], { stdio: "ignore", windowsHide: true });

try {
  const target = await waitForTarget(port);
  const cdp = await connectCdp(target.webSocketDebuggerUrl);
  const requests = [];
  const runtimeErrors = [];
  cdp.on("Network.requestWillBeSent", (event) => requests.push(event.request.url));
  cdp.on("Runtime.exceptionThrown", (event) => runtimeErrors.push(
    event.exceptionDetails.exception?.description ?? event.exceptionDetails.text,
  ));
  cdp.on("Log.entryAdded", (event) => {
    if (["error", "warning"].includes(event.entry.level)) runtimeErrors.push(event.entry.text);
  });
  await cdp.send("Page.enable");
  await cdp.send("Network.enable");
  await cdp.send("Runtime.enable");
  await cdp.send("Log.enable");
  await cdp.send("Emulation.setDeviceMetricsOverride", {
    width: 1600,
    height: 1000,
    deviceScaleFactor: 1,
    mobile: false,
  });
  await cdp.send("Page.navigate", { url: appUrl });
  await wait(8_000);

  await evaluate(cdp, `localStorage.setItem("xauai.market-timeframe", "M5")`);
  requests.length = 0;
  await cdp.send("Page.reload", { ignoreCache: true });
  await wait(8_000);

  const ui = await evaluate(cdp, `(() => {
    const text = document.body.innerText;
    const providerChips = [...document.querySelectorAll(".chart-provider")].map((node) => node.textContent.trim());
    const toolbarItems = [...document.querySelector(".chart-toolbar").children].map((node) => node.textContent.trim());
    const buttons = [...document.querySelectorAll(".chart-controls button")].map((node) => ({
      label: node.getAttribute("aria-label"),
      visibleText: node.textContent.trim(),
    }));
    const moduleLabelSize = getComputedStyle(document.querySelector(".module-launcher span")).fontSize;
    const moduleTitleSize = getComputedStyle(document.querySelector(".module-launcher strong")).fontSize;
    return {
      activeTimeframe: document.querySelector(".chart-timeframes button.active")?.textContent.trim(),
      providerChips,
      toolbarItems,
      chartButtons: buttons,
      hasLegacyUtcLabel: /(^|\\s)UTC(?!\\+7)/m.test(text),
      hasUtcPlusSeven: text.includes("UTC+7"),
      hasConvertedClose: /Close Sat (03:58|04:58) UTC\\+7/.test(text),
      moduleLabelSize,
      moduleTitleSize,
    };
  })()`);
  await evaluate(cdp, `([...document.querySelectorAll(".module-launcher")].find((node) => node.textContent.includes("Provider & storage"))).click()`);
  await wait(500);
  const providerTypography = await evaluate(cdp, `({
    providerNameSize: getComputedStyle(document.querySelector(".provider-health-row strong")).fontSize,
    providerDetailSize: getComputedStyle(document.querySelector(".provider-health-row small")).fontSize,
  })`);
  await evaluate(cdp, `document.querySelector(".control-modal-header button").click()`);

  const lineResult = await clickToolAndChart(cdp, "Line", [[0.35, 0.58], [0.62, 0.37]]);
  const freehandResult = await drawFreehand(cdp);
  const positionResult = await clickToolAndChart(cdp, "Position", [[0.48, 0.55]]);
  const positionDragResult = await dragChartLevel(cdp, [0.72, 0.517], [0.72, 0.64]);
  const screenshot = await cdp.send("Page.captureScreenshot", { format: "png", captureBeyondViewport: false });
  await writeFile(screenshotPath, Buffer.from(screenshot.data, "base64"));

  const timeframeRequests = requests.filter((url) => /market|analysis|analyst/i.test(url));
  const m15Requests = timeframeRequests.filter((url) => /(?:[?&]timeframe=|\/)(?:M15)(?:[/?&]|$)/i.test(url));
  const m5Requests = timeframeRequests.filter((url) => /(?:[?&]timeframe=|\/)(?:M5)(?:[/?&]|$)/i.test(url));
  console.log(JSON.stringify({
    ...ui,
    ...providerTypography,
    reloadNetwork: { m5Requests: m5Requests.length, m15Requests: m15Requests.length },
    lineResult,
    freehandResult,
    positionResult,
    positionDragResult,
    runtimeErrors: [...new Set(runtimeErrors)],
    screenshotPath,
  }, null, 2));
  cdp.close();
} finally {
  browser.kill();
  await Promise.race([
    new Promise((resolve) => browser.once("exit", resolve)),
    wait(2_000),
  ]);
  if (profile.startsWith(`${tmpdir()}${sep}xau-ui-smoke-`)) {
    try {
      await rm(profile, { recursive: true, force: true });
    } catch {
      // Edge crash reporting can briefly retain a file handle after shutdown.
    }
  }
}

async function clickToolAndChart(cdp, label, points) {
  await evaluate(cdp, `document.querySelector('.chart-controls button[aria-label=${JSON.stringify(label)}]').click()`);
  const toolStates = [];
  for (const [xRatio, yRatio] of points) {
    toolStates.push(await evaluate(cdp, `document.querySelector(".chart-wrap").dataset.drawingTool`));
    const rect = await evaluate(cdp, `(() => { const value = document.querySelector(".chart-canvas").getBoundingClientRect(); return { x: value.x, y: value.y, width: value.width, height: value.height }; })()`);
    const x = rect.x + rect.width * xRatio;
    const y = rect.y + rect.height * yRatio;
    await cdp.send("Input.dispatchMouseEvent", { type: "mousePressed", x, y, button: "left", clickCount: 1 });
    await cdp.send("Input.dispatchMouseEvent", { type: "mouseReleased", x, y, button: "left", clickCount: 1 });
    await wait(650);
  }
  return {
    toolStates,
    finalTool: await evaluate(cdp, `document.querySelector(".chart-wrap").dataset.drawingTool`),
    clearEnabled: await evaluate(cdp, `!document.querySelector('.chart-controls button[aria-label="Clear drawings"]').disabled`),
  };
}

async function drawFreehand(cdp) {
  await evaluate(cdp, `document.querySelector('.chart-controls button[aria-label="Freehand draw"]').click()`);
  const rect = await evaluate(cdp, `(() => { const value = document.querySelector(".chart-canvas").getBoundingClientRect(); return { x: value.x, y: value.y, width: value.width, height: value.height }; })()`);
  const points = [[0.30, 0.42], [0.36, 0.36], [0.42, 0.44], [0.49, 0.32]].map(([xRatio, yRatio]) => ({
    x: rect.x + rect.width * xRatio,
    y: rect.y + rect.height * yRatio,
  }));
  await cdp.send("Input.dispatchMouseEvent", { type: "mousePressed", ...points[0], button: "left", clickCount: 1 });
  for (const point of points.slice(1)) {
    await cdp.send("Input.dispatchMouseEvent", { type: "mouseMoved", ...point, button: "left", buttons: 1 });
  }
  await cdp.send("Input.dispatchMouseEvent", { type: "mouseReleased", ...points.at(-1), button: "left", clickCount: 1 });
  await wait(650);
  return {
    paths: await evaluate(cdp, `document.querySelectorAll(".chart-drawing-layer path").length`),
    finalTool: await evaluate(cdp, `document.querySelector(".chart-wrap").dataset.drawingTool`),
  };
}

async function dragChartLevel(cdp, [startXRatio, startYRatio], [endXRatio, endYRatio]) {
  const rect = await evaluate(cdp, `(() => { const value = document.querySelector(".chart-canvas").getBoundingClientRect(); return { x: value.x, y: value.y, width: value.width, height: value.height }; })()`);
  const start = { x: rect.x + rect.width * startXRatio, y: rect.y + rect.height * startYRatio };
  const end = { x: rect.x + rect.width * endXRatio, y: rect.y + rect.height * endYRatio };
  await cdp.send("Input.dispatchMouseEvent", { type: "mousePressed", ...start, button: "left", clickCount: 1 });
  await cdp.send("Input.dispatchMouseEvent", { type: "mouseMoved", ...end, button: "left", buttons: 1 });
  await cdp.send("Input.dispatchMouseEvent", { type: "mouseReleased", ...end, button: "left", clickCount: 1 });
  await wait(650);
  return { start, end };
}

async function evaluate(cdp, expression) {
  const result = await cdp.send("Runtime.evaluate", { expression, awaitPromise: true, returnByValue: true });
  if (result.exceptionDetails) {
    const detail = result.exceptionDetails.exception?.description ?? result.exceptionDetails.text;
    throw new Error(detail);
  }
  return result.result.value;
}

async function waitForTarget(port) {
  for (let attempt = 0; attempt < 60; attempt += 1) {
    try {
      const response = await fetch(`http://127.0.0.1:${port}/json/list`);
      const targets = await response.json();
      const page = targets.find((target) => target.type === "page");
      if (page) return page;
    } catch {
      // Browser is still starting.
    }
    await wait(250);
  }
  throw new Error("Headless browser did not expose a page target.");
}

async function connectCdp(url) {
  const socket = new WebSocket(url);
  const pending = new Map();
  const listeners = new Map();
  let id = 0;
  await new Promise((resolve, reject) => {
    socket.addEventListener("open", resolve, { once: true });
    socket.addEventListener("error", reject, { once: true });
  });
  socket.addEventListener("message", (message) => {
    const payload = JSON.parse(message.data);
    if (payload.id) {
      const entry = pending.get(payload.id);
      if (!entry) return;
      pending.delete(payload.id);
      if (payload.error) entry.reject(new Error(payload.error.message));
      else entry.resolve(payload.result ?? {});
      return;
    }
    for (const listener of listeners.get(payload.method) ?? []) listener(payload.params ?? {});
  });
  return {
    send(method, params = {}) {
      const requestId = ++id;
      socket.send(JSON.stringify({ id: requestId, method, params }));
      return new Promise((resolve, reject) => pending.set(requestId, { resolve, reject }));
    },
    on(method, listener) {
      listeners.set(method, [...(listeners.get(method) ?? []), listener]);
    },
    close() {
      socket.close();
    },
  };
}

async function findOpenPort() {
  const server = net.createServer();
  await new Promise((resolve, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", resolve);
  });
  const address = server.address();
  const port = typeof address === "object" && address ? address.port : 0;
  await new Promise((resolve) => server.close(resolve));
  return port;
}

function wait(milliseconds) {
  return new Promise((resolve) => setTimeout(resolve, milliseconds));
}
