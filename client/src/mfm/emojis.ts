import { api } from "../api.js";

export interface Emoji {
  name: string;
  url: string;
}

const TWEMOJI = "https://cdn.jsdelivr.net/gh/jdecked/twemoji@17.0.3/assets/svg";

export function twemoji(emoji: string): string {
  const text = emoji.includes("‍") ? emoji : emoji.replace(/️/g, "");
  const code = [...text].map((c) => c.codePointAt(0)!.toString(16)).join("-");
  return `${TWEMOJI}/${code}.svg`;
}

export async function loadEmojis(): Promise<Map<string, string>> {
  try {
    const emojis = await api<Emoji[]>("GET", "/api/emojis");
    return new Map(emojis.map((emoji) => [emoji.name, emoji.url]));
  } catch {
    return new Map();
  }
}
