const localDevHeaders: HeadersInit = import.meta.env.VITE_DEV_USER_ID
  ? {
      'X-Dev-User-Id': import.meta.env.VITE_DEV_USER_ID,
      ...(import.meta.env.VITE_DEV_PERMISSION ? { 'X-Dev-Permission': import.meta.env.VITE_DEV_PERMISSION } : {}),
    }
  : {}

export class ApiError extends Error {
  constructor(public readonly status: number, message = `API ${status}`) {
    super(message)
    this.name = 'ApiError'
  }
}

export async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(path, { headers: localDevHeaders })
  if (!response.ok) {
    throw new ApiError(response.status)
  }
  return response.json() as Promise<T>
}

export async function postJson<T>(path: string, body?: unknown): Promise<T> {
  const response = await fetch(path, {
    method: 'POST',
    headers: { ...localDevHeaders, 'Content-Type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (!response.ok) {
    throw new ApiError(response.status)
  }
  return response.json() as Promise<T>
}

export async function putJson<T>(path: string, body: unknown): Promise<T> {
  const response = await fetch(path, {
    method: 'PUT',
    headers: { ...localDevHeaders, 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  })
  if (!response.ok) {
    throw new ApiError(response.status)
  }
  return response.json() as Promise<T>
}
