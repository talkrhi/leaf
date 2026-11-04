export type Inline =
  | { type: "text"; text: string }
  | { type: "br" }
  | { type: "bold" | "italic" | "strike" | "small" | "center"; children: Inline[] }
  | { type: "code"; text: string }
  | { type: "link"; url: string; children: Inline[] }
  | { type: "url"; url: string }
  | { type: "image"; url: string; alt: string }
  | { type: "mention"; user: string; host: string | null }
  | { type: "hashtag"; tag: string }
  | { type: "emojiCode"; name: string }
  | { type: "unicodeEmoji"; emoji: string }
  | { type: "fn"; name: string; args: Record<string, string>; children: Inline[] };

export type Block =
  | { type: "paragraph"; children: Inline[] }
  | { type: "heading"; level: number; children: Inline[] }
  | { type: "quote"; children: Block[] }
  | { type: "code"; lang: string; text: string }
  | { type: "list"; items: Inline[][] }
  | { type: "embed"; url: string }
  | { type: "rule" };

const MAX_DEPTH = 16;

export function parse(source: string, depth = 0): Block[] {
  const lines = source.replace(/\r\n?/g, "\n").split("\n");
  const blocks: Block[] = [];
  let paragraph: string[] = [];

  const flush = () => {
    if (paragraph.length) blocks.push({ type: "paragraph", children: parseInline(paragraph.join("\n")) });
    paragraph = [];
  };

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    let m: RegExpExecArray | null;

    if ((m = /^```(\S*)\s*$/.exec(line))) {
      flush();
      const code: string[] = [];
      while (++i < lines.length && !/^```\s*$/.test(lines[i])) code.push(lines[i]);
      blocks.push({ type: "code", lang: m[1], text: code.join("\n") });
    } else if ((m = /^(#{1,3}) (.+)$/.exec(line))) {
      flush();
      blocks.push({ type: "heading", level: m[1].length, children: parseInline(m[2]) });
    } else if (line.startsWith(">") && depth < MAX_DEPTH) {
      flush();
      const quoted: string[] = [];
      for (; i < lines.length && lines[i].startsWith(">"); i++) quoted.push(lines[i].replace(/^> ?/, ""));
      i--;
      blocks.push({ type: "quote", children: parse(quoted.join("\n"), depth + 1) });
    } else if (/^-{3,}\s*$/.test(line)) {
      flush();
      blocks.push({ type: "rule" });
    } else if (/^[-*] /.test(line)) {
      flush();
      const items: Inline[][] = [];
      for (; i < lines.length && /^[-*] /.test(lines[i]); i++) items.push(parseInline(lines[i].slice(2)));
      i--;
      blocks.push({ type: "list", items });
    } else if ((m = /^\s*(https?:\/\/\S+)\s*$/.exec(line))) {
      flush();
      blocks.push({ type: "embed", url: m[1] });
    } else if (line.trim() === "") {
      flush();
    } else {
      paragraph.push(line);
    }
  }
  flush();
  return blocks;
}

type Match = { node: Inline; end: number } | null;

const ESCAPABLE = "\\*~`$[]<>#@:_";
const TAGS = { b: "bold", i: "italic", s: "strike", small: "small", center: "center" } as const;
const URL_TARGET = String.raw`(https?:\/\/[^\s)]+|\/[^\s)]*)`;

const FN = /\$\[([a-z0-9]+)(?:\.([^\s\]]*))?[ \n]/iy;
const TAG = /<(b|i|s|small|center|plain)>/y;
const ITALIC = /\*(?![\s*])([^*\n]+?)(?<!\s)\*/y;
const IMAGE = new RegExp(String.raw`!\[([^\]\n]*)\]\(${URL_TARGET}\)`, "y");
const LINK = new RegExp(String.raw`\??\[([^\]\n]+)\]\(${URL_TARGET}\)`, "y");
const BARE_URL = /https?:\/\/[^\s<>"'`]+/y;
const MENTION = /@([\w-]+)(?:@([\w-]+(?:\.[\w-]+)+))?/y;
const HASHTAG = /#([^\s.,!?'"#:\/\[\]()<>「」【】]+)/uy;
const EMOJI_CODE = /(?<![a-zA-Z0-9]):([a-z0-9_-]+):(?![a-zA-Z0-9])/y;
const EMOJI_PART = String.raw`(?:\p{Emoji_Presentation}|\p{Extended_Pictographic}(?=️|\p{Emoji_Modifier}))️?\p{Emoji_Modifier}?`;
const UNICODE_EMOJI = new RegExp(
  String.raw`[#*0-9]️?⃣|\p{Regional_Indicator}{2}|${EMOJI_PART}(?:[\u{E0020}-\u{E007E}]+\u{E007F}|(?:‍\p{Extended_Pictographic}️?\p{Emoji_Modifier}?)*)`,
  "uy",
);

export function parseInline(source: string, depth = 0): Inline[] {
  const nodes: Inline[] = [];
  let text = "";
  let i = 0;

  while (i < source.length) {
    const c = source[i];
    if (c === "\\" && ESCAPABLE.includes(source[i + 1] ?? "")) {
      text += source[i + 1];
      i += 2;
      continue;
    }

    const match: Match = c === "\n" ? { node: { type: "br" }, end: i + 1 } : depth < MAX_DEPTH ? matchAt(source, i, depth) : null;
    if (match) {
      if (text) nodes.push({ type: "text", text });
      text = "";
      nodes.push(match.node);
      i = match.end;
    } else {
      text += c;
      i++;
    }
  }

  if (text) nodes.push({ type: "text", text });
  return nodes;
}

function matchAt(s: string, i: number, depth: number): Match {
  let m: RegExpExecArray | null;
  if ((m = exec(UNICODE_EMOJI, s, i))) return { node: { type: "unicodeEmoji", emoji: m[0] }, end: UNICODE_EMOJI.lastIndex };
  switch (s[i]) {
    case "$":
      return fn(s, i, depth);
    case "*":
      return between(s, i, "**", (children) => ({ type: "bold", children }), depth) ?? italic(s, i, depth);
    case "~":
      return between(s, i, "~~", (children) => ({ type: "strike", children }), depth);
    case "`": {
      const end = s.indexOf("`", i + 1);
      if (end <= i + 1 || s.slice(i + 1, end).includes("\n")) return null;
      return { node: { type: "code", text: s.slice(i + 1, end) }, end: end + 1 };
    }
    case "<":
      return tag(s, i, depth);
    case "!":
      if (!(m = exec(IMAGE, s, i))) return null;
      return { node: { type: "image", alt: m[1], url: m[2] }, end: IMAGE.lastIndex };
    case "?":
    case "[":
      if (!(m = exec(LINK, s, i))) return null;
      return { node: { type: "link", url: m[2], children: parseInline(m[1], depth + 1) }, end: LINK.lastIndex };
    case "h":
      return bareUrl(s, i);
    case "@":
      if (isWordChar(s[i - 1]) || !(m = exec(MENTION, s, i))) return null;
      return { node: { type: "mention", user: m[1], host: m[2] ?? null }, end: MENTION.lastIndex };
    case "#":
      if (isWordChar(s[i - 1]) || !(m = exec(HASHTAG, s, i)) || /^\d+$/.test(m[1])) return null;
      return { node: { type: "hashtag", tag: m[1] }, end: HASHTAG.lastIndex };
    case ":":
      if (!(m = exec(EMOJI_CODE, s, i))) return null;
      return { node: { type: "emojiCode", name: m[1] }, end: EMOJI_CODE.lastIndex };
  }
  return null;
}

function exec(pattern: RegExp, s: string, i: number) {
  pattern.lastIndex = i;
  return pattern.exec(s);
}

const isWordChar = (c: string | undefined) => c !== undefined && /[\p{L}\p{N}_]/u.test(c);

function between(s: string, i: number, marker: string, make: (children: Inline[]) => Inline, depth: number): Match {
  if (!s.startsWith(marker, i)) return null;
  const start = i + marker.length;
  const end = s.indexOf(marker, start);
  if (end <= start) return null;
  return { node: make(parseInline(s.slice(start, end), depth + 1)), end: end + marker.length };
}

function italic(s: string, i: number, depth: number): Match {
  const m = isWordChar(s[i - 1]) ? null : exec(ITALIC, s, i);
  return m && { node: { type: "italic", children: parseInline(m[1], depth + 1) }, end: ITALIC.lastIndex };
}

function tag(s: string, i: number, depth: number): Match {
  const m = exec(TAG, s, i);
  if (!m) return null;
  const name = m[1];
  const close = `</${name}>`;
  const start = TAG.lastIndex;
  const end = s.indexOf(close, start);
  if (end < 0) return null;

  const inner = s.slice(start, end);
  const node: Inline =
    name === "plain"
      ? { type: "text", text: inner }
      : { type: TAGS[name as keyof typeof TAGS], children: parseInline(inner, depth + 1) };
  return { node, end: end + close.length };
}

function fn(s: string, i: number, depth: number): Match {
  const m = exec(FN, s, i);
  if (!m) return null;

  const start = FN.lastIndex;
  let level = 1;
  let j = start;
  for (; j < s.length; j++) {
    if (s[j] === "\\") j++;
    else if (s[j] === "[") level++;
    else if (s[j] === "]" && --level === 0) break;
  }
  if (level !== 0) return null;

  const args = Object.fromEntries(
    (m[2] ?? "").split(",").filter(Boolean).map((part) => {
      const [key, value = ""] = part.split("=", 2);
      return [key, value];
    }),
  );
  return {
    node: { type: "fn", name: m[1].toLowerCase(), args, children: parseInline(s.slice(start, j), depth + 1) },
    end: j + 1,
  };
}

function bareUrl(s: string, i: number): Match {
  const m = exec(BARE_URL, s, i);
  if (!m) return null;
  let url = m[0].replace(/[.,!?:;]+$/, "");
  if (url.endsWith(")") && !url.includes("(")) url = url.slice(0, -1);
  return { node: { type: "url", url }, end: i + url.length };
}
