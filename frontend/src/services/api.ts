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
