/**
 * VenueAxe Dynamic Auto-Resizing Iframe Widget SDK
 * Version: 1.0.0
 * Enables seamless, scrollbar-free embedding of VenueAxe booking wizards
 * into WordPress, Squarespace, Wix, Webflow, and custom websites.
 */
(function (window) {
  'use strict';

  function initWidgets() {
    var iframes = document.querySelectorAll('iframe[data-venueaxe-widget], iframe#venueaxe-booking');
    
    iframes.forEach(function (iframe) {
      // Ensure smooth height adjustments without double scrollbars
      iframe.style.border = 'none';
      iframe.style.width = '100%';
      iframe.style.display = 'block';
      iframe.style.overflow = 'hidden';
      iframe.style.transition = 'height 0.25s cubic-bezier(0.4, 0, 0.2, 1)';
      iframe.setAttribute('scrolling', 'no');
    });

    window.addEventListener('message', function (event) {
      if (!event.data || typeof event.data !== 'object') return;

      if (event.data.type === 'venueaxe:resize') {
        var height = parseInt(event.data.height, 10);
        if (height > 0) {
          iframes.forEach(function (iframe) {
            if (iframe.contentWindow === event.source || iframe.id === event.data.widgetId) {
              iframe.style.height = height + 'px';
            }
          });
        }
      }

      if (event.data.type === 'venueaxe:scroll-top') {
        iframes.forEach(function (iframe) {
          if (iframe.contentWindow === event.source) {
            iframe.scrollIntoView({ behavior: 'smooth', block: 'start' });
          }
        });
      }
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initWidgets);
  } else {
    initWidgets();
  }

  window.VenueAxe = {
    init: initWidgets
  };
})(window);
