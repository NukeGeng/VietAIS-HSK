# HSK Design Template

This folder contains the visual prototype for the HSK 3.0 platform.

It exists so the product interface can be reviewed in a real browser **before** production frontend/backend implementation starts.

## Why HTML/CSS first?

HTML/CSS is used because it gives a stable browser-rendered representation of:

- typography;
- spacing;
- responsive behavior;
- real text wrapping;
- Vietnamese diacritics;
- Chinese characters;
- Pinyin;
- hover states;
- transitions;
- section rhythm.

No Figma step is required.

## Purpose

```text
Product idea
    ↓
Static HTML/CSS prototype
    ↓
Browser review
    ↓
Visual comparison with VietAIS
    ↓
User approval
    ↓
Vue 3 implementation
```

## Folder structure

```text
design-template/
├── index.html
├── styles/
│   ├── tokens.css
│   ├── global.css
│   └── landing.css
├── scripts/
│   └── main.js
├── assets/
│   └── README.md
└── docs/
    ├── DESIGN_RULES.md
    ├── VIETAIS_REFERENCE.md
    └── VISUAL_QA.md
```

## Run locally

Option 1:

```bash
cd design-template
python3 -m http.server 8080
```

Open:

```text
http://localhost:8080
```

Option 2:

```bash
npx serve .
```

## Important

Do not add:

- API clients;
- backend calls;
- authentication logic;
- database logic;
- state management;
- RabbitMQ;
- Marten;
- Wolverine.

This folder is only for design validation.

# VietAIS-HSK
