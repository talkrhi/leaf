import { h } from "../dom.js";
import { embed } from "./embeds.js";
import { twemoji } from "./emojis.js";
import { parse, type Block, type Inline } from "./parser.js";

export interface RenderOptions {
  media?: boolean;
  emojis?: Map<string, string>;
}

export function renderMfm(source: string, options: RenderOptions = {}): DocumentFragment {
  const media = options.media ?? true;
  const emojis = options.emojis ?? new Map<string, string>();
  const fragment = document.createDocumentFragment();

  const inline = (node: Inline): Node => renderInline(node, inline, media, emojis);
  for (const block of parse(source)) fragment.append(renderBlock(block, inline, media));
  return fragment;
}

type InlineRenderer = (node: Inline) => Node;

function renderBlock(block: Block, inline: InlineRenderer, media: boolean): Node {
  switch (block.type) {
    case "paragraph":
      return h("p", {}, block.children.map(inline));
    case "heading":
      return h(`h${block.level + 1}` as "h2", {}, block.children.map(inline));
    case "quote":
      return h("blockquote", {}, block.children.map((child) => renderBlock(child, inline, media)));
    case "code":
      return h("pre", { "data-lang": block.lang || null }, h("code", {}, block.text));
    case "list":
      return h("ul", {}, block.items.map((item) => h("li", {}, item.map(inline))));
    case "embed":
      return (media && embed(block.url)) || h("p", {}, inline({ type: "url", url: block.url }));
    case "rule":
      return h("hr");
  }
}

function renderInline(node: Inline, inline: InlineRenderer, media: boolean, emojis: Map<string, string>): Node {
  const children = "children" in node ? node.children.map(inline) : [];
  switch (node.type) {
    case "text":
      return document.createTextNode(node.text);
    case "br":
      return h("br");
    case "bold":
      return h("strong", {}, children);
    case "italic":
      return h("em", {}, children);
    case "strike":
      return h("del", {}, children);
    case "small":
      return h("small", { class: "mfm-small" }, children);
    case "center":
      return h("span", { class: "mfm-center" }, children);
    case "code":
      return h("code", {}, node.text);
    case "link":
      return link(node.url, children);
    case "url":
      return link(node.url, [node.url]);
    case "image":
      return media ? h("img", { src: node.url, alt: node.alt, loading: "lazy" }) : link(node.url, [node.alt || node.url]);
    case "mention":
      return node.host
        ? link(`https://${node.host}/@${node.user}`, [`@${node.user}@${node.host}`], "mfm-mention")
        : h("span", { class: "mfm-mention" }, `@${node.user}`);
    case "hashtag":
      return h("a", { class: "mfm-hashtag", href: `/?tag=${encodeURIComponent(node.tag.toLowerCase())}` }, `#${node.tag}`);
    case "emojiCode": {
      const code = `:${node.name}:`;
      const url = emojis.get(node.name);
      return url
        ? h("span", { class: "mfm-emoji-wrap", "data-name": code },
            h("img", { class: "mfm-emoji mfm-emoji-custom", src: url, alt: code }))
        : document.createTextNode(code);
    }
    case "unicodeEmoji":
      return h("img", {
        class: "mfm-emoji",
        src: twemoji(node.emoji),
        alt: node.emoji,
        onerror: (event) => (event.target as HTMLImageElement).replaceWith(node.emoji),
      });
    case "fn":
      return renderFn(node.name, node.args, children);
  }
}

function link(url: string, children: (Node | string)[], className?: string): Node {
  const external = /^https?:/.test(url);
  return h("a", { href: url, class: className, target: external ? "_blank" : null, rel: external ? "nofollow noopener" : null }, children);
}

const TIME = /^\d+(\.\d+)?m?s$/;
const HEX = /^[0-9a-f]{3,8}$/i;

const ANIMATIONS = new Map<string, [string, string, string]>([
  ["tada", ["mfm-tada", "1s", "linear"]],
  ["jelly", ["mfm-jelly", "1s", "linear"]],
  ["twitch", ["mfm-twitch", "0.5s", "ease"]],
  ["shake", ["mfm-shake", "0.5s", "ease"]],
  ["spin", ["mfm-spin", "1.5s", "linear"]],
  ["jump", ["mfm-jump", "0.75s", "linear"]],
  ["bounce", ["mfm-bounce", "0.75s", "linear"]],
  ["rainbow", ["mfm-rainbow", "1s", "linear"]],
  ["sparkle", ["mfm-sparkle", "1.5s", "ease-in-out"]],
]);

const BORDER_STYLES = ["solid", "dashed", "dotted", "double", "groove", "ridge", "inset", "outset"];
const FONTS = ["serif", "monospace", "cursive", "fantasy"];

function num(value: string | undefined, fallback: number, min: number, max: number): number {
  const n = Number.parseFloat(value ?? "");
  return Number.isFinite(n) ? Math.min(max, Math.max(min, n)) : fallback;
}

function renderFn(name: string, args: Record<string, string>, children: Node[]): Node {
  const el = h("span", { class: `mfm-fn mfm-${name}` }, children);
  const style = el.style;
  const has = (key: string) => Object.hasOwn(args, key);

  const animation = ANIMATIONS.get(name);
  if (animation) {
    let [keyframes, speed, timing] = animation;
    if (name === "spin") keyframes += has("x") ? "X" : has("y") ? "Y" : "";
    if (TIME.test(args.speed ?? "")) speed = args.speed;
    const delay = TIME.test(args.delay ?? "") ? args.delay : "0s";
    const direction = has("left") ? "reverse" : has("alternate") ? "alternate" : "normal";
    style.animation = `${keyframes} ${speed} ${timing} ${delay} infinite ${direction}`;
    return el;
  }

  switch (name) {
    case "flip":
      style.transform = has("h") && has("v") ? "scale(-1, -1)" : has("v") ? "scaleY(-1)" : "scaleX(-1)";
      break;
    case "x2":
    case "x3":
    case "x4":
      style.fontSize = `${name[1]}em`;
      break;
    case "font":
      style.fontFamily = FONTS.find(has) ?? "inherit";
      break;
    case "rotate":
      style.transform = `rotate(${num(args.deg, 90, -360, 360)}deg)`;
      break;
    case "position":
      style.transform = `translate(${num(args.x, 0, -5, 5)}em, ${num(args.y, 0, -5, 5)}em)`;
      break;
    case "scale":
      style.transform = `scale(${num(args.x, 1, -5, 5)}, ${num(args.y, 1, -5, 5)})`;
      break;
    case "fg":
      if (HEX.test(args.color ?? "")) style.color = `#${args.color}`;
      break;
    case "bg":
      if (HEX.test(args.color ?? "")) style.backgroundColor = `#${args.color}`;
      break;
    case "border": {
      const color = HEX.test(args.color ?? "") ? `#${args.color}` : "currentColor";
      const kind = BORDER_STYLES.includes(args.style ?? "") ? args.style : "solid";
      style.border = `${num(args.width, 1, 0, 20)}px ${kind} ${color}`;
      style.borderRadius = `${num(args.radius, 0, 0, 100)}px`;
      break;
    }
    case "blur":
      break;
    case "ruby": {
      const [base = "", ...reading] = (el.textContent ?? "").trim().split(/\s+/);
      return h("ruby", {}, base, h("rt", {}, reading.join(" ")));
    }
    case "unixtime": {
      const date = new Date(Number(el.textContent) * 1000);
      if (Number.isNaN(date.getTime())) return el;
      return h("time", { datetime: date.toISOString() }, date.toLocaleString());
    }
    default:
      el.className = "";
  }
  return el;
}
