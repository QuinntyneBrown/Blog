export interface Page<T> {
    totalPages: number,
    currentPage: number,
    length: number,
    entities: T[]
};
