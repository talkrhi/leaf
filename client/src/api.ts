export class ApiError extends Error {
  status: number;

  constructor(status: number, message: string) {
    super(message);
    this.status = status;
  }
}

export async function api<T = void>(method: string, url: string, body?: unknown): Promise<T> {
  const raw = body === undefined || body instanceof FormData;
  const response = await fetch(url, {
    method,
    headers: raw ? {} : { "Content-Type": "application/json" },
    body: raw ? body : JSON.stringify(body),
  });

  const text = await response.text();
  let data: any;
  try {
    data = text ? JSON.parse(text) : undefined;
  } catch {
    data = undefined;
  }

  if (!response.ok) {
    if (response.status === 429) throw new ApiError(429, "Too many tries. Wait a minute and try again.");
    throw new ApiError(response.status, data?.error ?? data?.title ?? `Something went wrong (${response.status}).`);
  }
  return data as T;
}

export const errorText = (error: unknown) => (error instanceof Error ? error.message : String(error));
