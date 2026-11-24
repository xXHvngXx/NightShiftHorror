<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1280 340" width="1280" height="340" role="img" aria-label="Ca Trực Đêm - Night Shift Horror">
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#090104" />
      <stop offset="0.55" stop-color="#26050e" />
      <stop offset="1" stop-color="#4a0919" />
    </linearGradient>
    <radialGradient id="vig" cx="0.5" cy="0.5" r="0.75">
      <stop offset="0.45" stop-color="#000" stop-opacity="0" />
      <stop offset="1" stop-color="#000" stop-opacity="0.9" />
    </radialGradient>
    <radialGradient id="glow" cx="0.5" cy="0.5" r="0.5">
      <stop offset="0" stop-color="#e11d48" stop-opacity="0.55" />
      <stop offset="1" stop-color="#e11d48" stop-opacity="0" />
    </radialGradient>
    <linearGradient id="txt" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#ffe4e6" />
      <stop offset="0.6" stop-color="#fda4af" />
      <stop offset="1" stop-color="#be123c" />
    </linearGradient>
    <linearGradient id="blood" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="#7f1128" />
      <stop offset="1" stop-color="#e11d48" />
    </linearGradient>
    <filter id="fog" x="-20%" y="-50%" width="140%" height="200%"><feGaussianBlur stdDeviation="28" /></filter>
    <pattern id="scan" width="4" height="4" patternUnits="userSpaceOnUse">
      <rect width="4" height="1.4" fill="#000" opacity="0.35" />
    </pattern>
  </defs>

  <style>
    .title { font-family: Georgia, 'Times New Roman', serif; font-weight: 900; font-size: 92px; letter-spacing: 12px; text-anchor: middle; }
    .sub   { font-family: 'Courier New', Consolas, monospace; font-weight: 700; font-size: 22px; letter-spacing: 9px; text-anchor: middle; fill: #fb7185; }
    .tag   { font-family: 'Courier New', Consolas, monospace; font-size: 15px; letter-spacing: 4px; text-anchor: middle; fill: #9f1239; }

    .main   { animation: flicker 6s infinite; }
    .ghostA { fill: #ff2a52; opacity: 0; animation: glitchA 6s infinite; }
    .ghostB { fill: #7dd3fc; opacity: 0; animation: glitchB 6s infinite; }

    @keyframes flicker {
      0%, 18%, 22%, 62%, 64%, 100% { opacity: 1; }
      19% { opacity: 0.25; } 20% { opacity: 0.9; } 21% { opacity: 0.15; }
      63% { opacity: 0.4; }
      80% { opacity: 1; } 81% { opacity: 0.55; } 82% { opacity: 1; }
    }
    @keyframes glitchA {
      0%, 88%, 100% { opacity: 0; transform: translate(0, 0); }
      89% { opacity: .85; transform: translate(-7px, 1px); }
      91% { opacity: .85; transform: translate(6px, -2px); }
      93% { opacity: .7;  transform: translate(-3px, 2px); }
      95% { opacity: 0; }
    }
    @keyframes glitchB {
      0%, 88%, 100% { opacity: 0; transform: translate(0, 0); }
      90% { opacity: .6; transform: translate(7px, -1px); }
      92% { opacity: .6; transform: translate(-6px, 2px); }
      94% { opacity: 0; }
    }

    .fog1 { animation: drift1 18s ease-in-out infinite alternate; }
    .fog2 { animation: drift2 24s ease-in-out infinite alternate; }
    .fog3 { animation: drift1 30s ease-in-out infinite alternate-reverse; }
    @keyframes drift1 { from { transform: translateX(-120px); } to { transform: translateX(140px); } }
    @keyframes drift2 { from { transform: translateX(150px); }  to { transform: translateX(-130px); } }

    .scan { animation: scan 7s linear infinite; }
    @keyframes scan { from { transform: translateY(-4px); } to { transform: translateY(0); } }

    .pulse { animation: pulse 3.2s ease-in-out infinite; transform-origin: 640px 170px; }
    @keyframes pulse { 0%, 100% { opacity: .55; transform: scale(1); } 50% { opacity: 1; transform: scale(1.12); } }

    .eye { fill: #ff1744; transform-box: fill-box; transform-origin: center; animation: look 9s infinite; }
    @keyframes look {
      0%, 30% { opacity: 0; transform: scaleY(1); }
      34%, 52% { opacity: 1; transform: scaleY(1); }
      40% { transform: scaleY(0.05); }
      42% { transform: scaleY(1); }
      58%, 100% { opacity: 0; }
    }

    .tagblink { animation: tagblink 4s infinite; }
    @keyframes tagblink { 0%, 100% { opacity: .9; } 47% { opacity: .9; } 48% { opacity: .1; } 50% { opacity: .9; } 70% { opacity: .35; } 72% { opacity: .9; } }
  </style>

  
  <rect width="1280" height="340" fill="url(#bg)" />

  
  <g filter="url(#fog)">
    <ellipse class="fog1" cx="320" cy="270" rx="360" ry="46" fill="#7f1128" opacity="0.45" />
    <ellipse class="fog2" cx="900" cy="300" rx="420" ry="52" fill="#9f1239" opacity="0.32" />
    <ellipse class="fog3" cx="640" cy="230" rx="300" ry="34" fill="#4a0919" opacity="0.55" />
  </g>

  
  <ellipse class="pulse" cx="640" cy="170" rx="470" ry="120" fill="url(#glow)" />

  
  <g>
    <rect x="118" y="0" width="5" height="0" rx="2.5" fill="url(#blood)">
      <animate attributeName="height" values="0;70;70;0" keyTimes="0;0.5;0.9;1" dur="7s" repeatCount="indefinite" />
    </rect>
    <circle cx="120.5" cy="0" r="6" fill="#e11d48">
      <animate attributeName="cy" values="0;70;70;0" keyTimes="0;0.5;0.9;1" dur="7s" repeatCount="indefinite" />
    </circle>

    <rect x="402" y="0" width="4" height="0" rx="2" fill="url(#blood)">
      <animate attributeName="height" values="0;110;110;0" keyTimes="0;0.55;0.92;1" dur="9s" begin="1.5s" repeatCount="indefinite" />
    </rect>
    <circle cx="404" cy="0" r="5" fill="#e11d48">
      <animate attributeName="cy" values="0;110;110;0" keyTimes="0;0.55;0.92;1" dur="9s" begin="1.5s" repeatCount="indefinite" />
    </circle>

    <rect x="868" y="0" width="5" height="0" rx="2.5" fill="url(#blood)">
      <animate attributeName="height" values="0;86;86;0" keyTimes="0;0.5;0.9;1" dur="8s" begin="3s" repeatCount="indefinite" />
    </rect>
    <circle cx="870.5" cy="0" r="6" fill="#e11d48">
      <animate attributeName="cy" values="0;86;86;0" keyTimes="0;0.5;0.9;1" dur="8s" begin="3s" repeatCount="indefinite" />
    </circle>

    <rect x="1154" y="0" width="4" height="0" rx="2" fill="url(#blood)">
      <animate attributeName="height" values="0;58;58;0" keyTimes="0;0.5;0.9;1" dur="6s" begin="0.8s" repeatCount="indefinite" />
    </rect>
    <circle cx="1156" cy="0" r="5" fill="#e11d48">
      <animate attributeName="cy" values="0;58;58;0" keyTimes="0;0.5;0.9;1" dur="6s" begin="0.8s" repeatCount="indefinite" />
    </circle>
  </g>

  
  <g>
    <text class="title ghostB" x="640" y="188">CA TRỰC ĐÊM</text>
    <text class="title ghostA" x="640" y="188">CA TRỰC ĐÊM</text>
    <text class="title main" x="640" y="188" fill="url(#txt)">CA TRỰC ĐÊM</text>
  </g>

  <text class="sub" x="640" y="238">NIGHT SHIFT HORROR  •  v1.7</text>
  <text class="tag tagblink" x="640" y="274">00:00 — 05:00  ·  ĐƯỜNG SỐ 9  ·  ĐỪNG MỞ CỬA SAU 03:00</text>

  
  <g>
    <ellipse class="eye" cx="1110" cy="104" rx="9" ry="4.5" />
    <ellipse class="eye" cx="1142" cy="104" rx="9" ry="4.5" />
  </g>

  
  <rect class="scan" width="1280" height="344" fill="url(#scan)" />
  <rect width="1280" height="340" fill="url(#vig)" />
</svg>
