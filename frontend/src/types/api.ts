export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
  errors?: string[];
  pagination?: PaginationMetadata;
}

export interface PaginationMetadata {
  totalCount: number;
  skip: number;
  take: number;
  hasMore: boolean;
}
