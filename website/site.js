(() => {
  const menu = document.querySelector('.menu-toggle');
  menu?.addEventListener('click', () => {
    const open = menu.getAttribute('aria-expanded') !== 'true';
    menu.setAttribute('aria-expanded', String(open));
    document.querySelector('#site-menu').classList.toggle('open', open);
  });
  document.addEventListener('keydown', e => {
    if (e.key === 'Escape' && menu) {
      menu.setAttribute('aria-expanded', 'false');
      document.querySelector('#site-menu').classList.remove('open');
    }
  });
  document.querySelectorAll('.languages a').forEach(a => a.addEventListener('click', () => {
    try { localStorage.setItem('studio.site.language', a.lang); } catch (_) {}
  }));
  if (['localhost', '127.0.0.1'].includes(location.hostname)) {
    const files = {setup:'YouTubeDownloader-PeakMP3-2.6.0-Preview-Setup.exe', zip:'YouTubeDownloader-PeakMP3-2.6.0-Preview-FULL.zip', sha:'SHA256SUMS.txt'};
    document.querySelectorAll('[data-download]').forEach(a => {
      a.href = 'dist/' + files[a.dataset.download];
    });
  }
})();
