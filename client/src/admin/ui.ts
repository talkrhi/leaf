import { h } from "../dom.js";

export const field = (label: string, input: HTMLElement) => h("label", {}, label, input);

export function setStatus(el: HTMLElement, text: string, ok = false) {
  el.textContent = text;
  el.className = ok ? "form-ok" : "form-error";
}

export const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });

export function allowTabs(textarea: HTMLTextAreaElement) {
  textarea.addEventListener("keydown", (event) => {
    if (event.key !== "Tab" || event.shiftKey) return;
    event.preventDefault();
    textarea.setRangeText("  ", textarea.selectionStart, textarea.selectionEnd, "end");
  });
}

export function saveShortcut(form: HTMLFormElement) {
  form.addEventListener("keydown", (event) => {
    if ((event.metaKey || event.ctrlKey) && event.key === "s") {
      event.preventDefault();
      form.requestSubmit();
    }
  });
}
