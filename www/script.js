/**
 * Browy Product Landing Page Interactive Script
 */

document.addEventListener('DOMContentLoaded', () => {
  // 1. FAQ Accordion Toggle
  const faqItems = document.querySelectorAll('.faq-item');
  faqItems.forEach(item => {
    const questionBtn = item.querySelector('.faq-question');
    questionBtn.addEventListener('click', () => {
      const isOpen = item.classList.contains('open');
      // Close other items
      faqItems.forEach(other => other.classList.remove('open'));
      if (!isOpen) {
        item.classList.add('open');
      }
    });
  });

  // Open first FAQ by default
  if (faqItems.length > 0) {
    faqItems[0].classList.add('open');
  }

  // 2. Interactive Mockup Spaces Switcher
  const spaceButtons = document.querySelectorAll('.space-btn');
  const spaceBadge = document.querySelector('.space-badge');
  const mockTabs = document.querySelectorAll('.mock-tab');

  const spaceThemes = {
    work: { name: 'Work Space', color: '#60A5FA', border: 'rgba(59, 130, 246, 0.4)', bg: 'rgba(59, 130, 246, 0.2)' },
    dev: { name: 'Developer Space', color: '#A78BFA', border: 'rgba(139, 92, 246, 0.4)', bg: 'rgba(139, 92, 246, 0.2)' },
    research: { name: 'Research Space', color: '#34D399', border: 'rgba(16, 185, 129, 0.4)', bg: 'rgba(16, 185, 129, 0.2)' },
    media: { name: 'Media Space', color: '#F472B6', border: 'rgba(236, 72, 153, 0.4)', bg: 'rgba(236, 72, 153, 0.2)' }
  };

  spaceButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      spaceButtons.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');

      const spaceKey = btn.getAttribute('data-space');
      const theme = spaceThemes[spaceKey];
      if (theme && spaceBadge) {
        spaceBadge.textContent = theme.name;
        spaceBadge.style.color = theme.color;
        spaceBadge.style.borderColor = theme.border;
        spaceBadge.style.background = theme.bg;
      }
    });
  });

  // 3. Interactive Mockup Tab Selection
  mockTabs.forEach(tab => {
    tab.addEventListener('click', () => {
      mockTabs.forEach(t => t.classList.remove('active'));
      tab.classList.add('active');
      const label = tab.querySelector('.tab-label');
      const titleText = document.querySelector('.tab-title-text');
      if (label && titleText) {
        titleText.textContent = label.textContent + ' — Browy Browser';
      }
    });
  });

  // 4. Copilot Quick Chips Interaction
  const quickChips = document.querySelectorAll('.copilot-quick-chips span');
  const mockInput = document.querySelector('.mock-input-box input');
  const chatContainer = document.querySelector('.copilot-chat');

  if (quickChips.length > 0 && mockInput) {
    quickChips.forEach(chip => {
      chip.addEventListener('click', () => {
        const text = chip.textContent;
        mockInput.value = text;
        simulateAgentResponse(text);
      });
    });
  }

  function simulateAgentResponse(query) {
    if (!chatContainer) return;

    // Add user message
    const userMsg = document.createElement('div');
    userMsg.className = 'chat-msg user';
    userMsg.innerHTML = `<div class="msg-bubble">${escapeHtml(query)}</div>`;
    chatContainer.appendChild(userMsg);

    // Simulate Agent typing and response
    setTimeout(() => {
      const agentMsg = document.createElement('div');
      agentMsg.className = 'chat-msg agent';
      let content = '';

      if (query.includes('Summarize')) {
        content = `<p><strong>Executive Summary:</strong></p>
                   <p>Browy delivers a clean, tab-isolated experience built on .NET 9 and WebView2 with 40% memory reduction.</p>
                   <span class="action-pill">✔ Scrolled &amp; highlighted key concepts</span>`;
      } else if (query.includes('Translate')) {
        content = `<p><strong>Traduction en Français:</strong></p>
                   <p>Browy est le navigateur le plus moderne pour Windows 11 avec des espaces de travail fluides.</p>
                   <span class="action-pill">✔ Autonomous Action: Translated active page</span>`;
      } else {
        content = `<p><strong>DOM Technical Analysis:</strong></p>
                   <p>Identified TLS 1.3 encryption, 4 active modules, and 0 third-party trackers detected.</p>
                   <span class="action-pill">✔ Inspected DOM tree</span>`;
      }

      agentMsg.innerHTML = `<div class="msg-bubble">${content}</div>`;
      chatContainer.appendChild(agentMsg);
      chatContainer.scrollTop = chatContainer.scrollHeight;
    }, 450);
  }

  function escapeHtml(str) {
    const div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
  }

  // 5. Fetch GitHub Stars dynamically
  const starCountEl = document.getElementById('starCount');
  if (starCountEl) {
    fetch('https://api.github.com/repos/tomlin7/Browy')
      .then(res => res.ok ? res.json() : null)
      .then(data => {
        if (data && data.stargazers_count !== undefined) {
          starCountEl.textContent = `★ ${data.stargazers_count.toLocaleString()}`;
        }
      })
      .catch(() => {
        // Fallback default
        starCountEl.textContent = '★ 1.2k';
      });
  }

  // 6. Smooth Scroll Navbar Spy
  const navLinks = document.querySelectorAll('.nav-links a');
  window.addEventListener('scroll', () => {
    let current = '';
    const sections = document.querySelectorAll('section[id]');
    sections.forEach(section => {
      const sectionTop = section.offsetTop - 120;
      if (window.scrollY >= sectionTop) {
        current = section.getAttribute('id');
      }
    });

    navLinks.forEach(link => {
      link.classList.remove('active');
      if (link.getAttribute('href') === `#${current}`) {
        link.classList.add('active');
      }
    });
  });
});
