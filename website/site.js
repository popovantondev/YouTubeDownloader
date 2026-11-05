(() => {
  // Local preview: download buttons point at files in dist/ (names are taken from the release links).
  if (['localhost', '127.0.0.1'].includes(location.hostname)) {
    document.querySelectorAll('[data-download]').forEach(a => {
      a.href = 'dist/' + decodeURIComponent(a.getAttribute('href').split('/').pop());
    });
  }
  // Fit the real popup preview to its contents, including expanded settings.
  document.querySelectorAll('.extension-frame iframe').forEach(frame => {
    frame.addEventListener('load', () => {
      const body = frame.contentDocument && frame.contentDocument.body;
      if (!body) return;
      const resize = () => { frame.style.height = Math.ceil(body.getBoundingClientRect().height + 2) + 'px'; };
      new ResizeObserver(resize).observe(body);
      resize();
    });
  });
})();
