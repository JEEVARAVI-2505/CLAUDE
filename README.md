# 3D Building MVP

A 12-storey glass tower built procedurally with Three.js (vendored, no build step or internet needed).

## Run

```bash
python3 -m http.server 8000
```

Open http://localhost:8000. ES modules need a server, so opening `index.html` directly won't work.

## Features

- Orbit / zoom / pan, auto-rotate
- Isolate any floor, or view the whole tower
- Roof cutaway toggle
- Day / night mode with lit windows

Edit the constants at the top of `app.js` (`FLOORS`, `W`, `D`, `FLOOR_H`) to change the building.
