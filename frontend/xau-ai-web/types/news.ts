export interface NewsArticle {
  id: string;
  provider: string;
  title: string;
  description: string | null;
  url: string;
  sourceName: string | null;
  publishedAtUtc: string | null;
  categories: string[];
  entities: string[];
  relevance: string;
  freshness: string;
}

export interface PagedNewsArticles {
  items: NewsArticle[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}

export interface NewsSystemStatus {
  provider: {
    provider: string;
    state: string;
    enabled: boolean;
    message: string;
    checkedAtUtc: string;
  };
  collection: {
    status: string;
    lastSuccessfulCollectionAtUtc: string | null;
    consecutiveFailures: number;
    lastErrorMessage: string | null;
  } | null;
  storedArticles: number;
}
