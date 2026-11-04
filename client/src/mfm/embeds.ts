import { h } from "../dom.js";

export function embed(link: string): HTMLElement | null {
  let url: URL;
  try {
    url = new URL(link);
  } catch {
    return null;
  }

  const host = url.hostname.replace(/^(www|m)\./, "");
  const [first = "", second = "", third = ""] = url.pathname.split("/").filter(Boolean);
  const found = find(url, host, first, second, third);
  if (!found) return null;

  const [src, kind] = found;
  return h(
    "div",
    { class: `mfm-embed mfm-embed-${kind}` },
    h("iframe", {
      src,
      loading: "lazy",
      allowfullscreen: true,
      referrerpolicy: "strict-origin-when-cross-origin",
      allow: "autoplay; encrypted-media; picture-in-picture; fullscreen",
      sandbox: "allow-scripts allow-same-origin allow-popups allow-presentation",
    }),
  );
}

type Kind = "video" | "audio" | "playlist" | "post";

function find(url: URL, host: string, first: string, second: string, third: string): [string, Kind] | null {
  const youtube = (id: string | null) =>
    id && /^[\w-]{11}$/.test(id) ? ([`https://www.youtube-nocookie.com/embed/${id}`, "video"] as [string, Kind]) : null;

  if (host === "youtube.com" && first === "watch") return youtube(url.searchParams.get("v"));
  if (host === "youtube.com" && first === "shorts") return youtube(second);
  if (host === "youtu.be") return youtube(first);

  if (host === "vimeo.com" && /^\d+$/.test(first)) return [`https://player.vimeo.com/video/${first}`, "video"];

  if (host === "open.spotify.com" && /^(track|album|playlist|episode|show)$/.test(first) && /^\w+$/.test(second)) {
    const kind = first === "track" || first === "episode" ? "audio" : "playlist";
    return [`https://open.spotify.com/embed/${first}/${second}`, kind];
  }

  if ((host === "x.com" || host === "twitter.com") && second === "status" && /^\d+$/.test(third)) {
    return [`https://platform.twitter.com/embed/Tweet.html?id=${third}`, "post"];
  }

  if (first === "notes" && /^[a-z0-9]+$/i.test(second) && !third) {
    return [`https://${url.host}/embed/notes/${second}`, "post"];
  }

  if (/^@[\w.]+$/.test(first) && /^\d+$/.test(second) && !third) {
    return [`https://${url.host}/${first}/${second}/embed`, "post"];
  }

  return null;
}
