const localDevHeaders: HeadersInit = import.meta.env.VITE_DEV_USER_ID
  ? { 'X-Dev-User-Id': import.meta.env.VITE_DEV_USER_ID }
  : {}

export async function getJson<T>(path: string): Promise<T> {
  const response = await fetch(path, { headers: localDevHeaders })
  if (!response.ok) {
    throw new Error(`API ${response.status}`)
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
    throw new Error(`API ${response.status}`)
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
    throw new Error(`API ${response.status}`)
  }
  return response.json() as Promise<T>
}
