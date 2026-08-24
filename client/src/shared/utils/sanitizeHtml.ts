const DIV = typeof document !== 'undefined' ? document.createElement('div') : null

export function sanitizeHtml(dirty: string): string {
  if (!DIV) return dirty
  DIV.textContent = dirty
  return DIV.innerHTML
}
