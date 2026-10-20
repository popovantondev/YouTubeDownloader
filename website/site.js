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

// Fit the actual popup preview to its contents, including expanded settings.
document.querySelectorAll('.extension-frame iframe').forEach(frame=>{
 frame.addEventListener('load',()=>{
  const body=frame.contentDocument?.body;if(!body)return;
  const resize=()=>{frame.style.height=Math.ceil(body.getBoundingClientRect().height+2)+'px';};
  new ResizeObserver(resize).observe(body);resize();
 });
});

// Keep cross-product navigation inside the two local review sites.
(() => {
 if (!['127.0.0.1','localhost'].includes(location.hostname)) return;
 for (const link of document.querySelectorAll('a[href]')) {
  const url = new URL(link.href, location.href);
  if (url.origin !== 'https://popovantondev.github.io') continue;
  const target = url.pathname.match(/^\/(PeakMP3|YouTubeDownloader)\/(.*)$/);
  if (!target) continue;
  link.href = 'http://' + location.hostname + ':' + (target[1] === 'PeakMP3' ? '8768' : '8767') + '/' + target[2] + url.search + url.hash;
 }
})();
