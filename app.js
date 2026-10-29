import * as THREE from 'three';
import { OrbitControls } from 'three/addons/OrbitControls.js';

const FLOORS = 12;
const FLOOR_H = 3.2;
const W = 20;
const D = 14;
const SLAB = 0.3;
const HOME_CAM = new THREE.Vector3(38, 26, 42);

const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera(50, innerWidth / innerHeight, 0.1, 500);
camera.position.copy(HOME_CAM);

const renderer = new THREE.WebGLRenderer({ antialias: true });
renderer.setPixelRatio(Math.min(devicePixelRatio, 2));
renderer.setSize(innerWidth, innerHeight);
renderer.shadowMap.enabled = true;
renderer.shadowMap.type = THREE.PCFSoftShadowMap;
document.body.appendChild(renderer.domElement);

const controls = new OrbitControls(camera, renderer.domElement);
controls.target.set(0, (FLOORS * FLOOR_H) / 2, 0);
controls.enableDamping = true;
controls.maxPolarAngle = Math.PI / 2 - 0.02;
controls.minDistance = 10;
controls.maxDistance = 150;
controls.autoRotate = true;
controls.autoRotateSpeed = 0.6;

// --- Lighting ---
const hemi = new THREE.HemisphereLight(0xcfe3ff, 0x3a3a44, 0.6);
scene.add(hemi);
const sun = new THREE.DirectionalLight(0xffffff, 2.2);
sun.position.set(40, 60, 25);
sun.castShadow = true;
sun.shadow.mapSize.set(2048, 2048);
Object.assign(sun.shadow.camera, { left: -50, right: 50, top: 50, bottom: -50, near: 1, far: 200 });
scene.add(sun);

// --- Ground ---
const ground = new THREE.Mesh(
  new THREE.CircleGeometry(120, 64),
  new THREE.MeshStandardMaterial({ color: 0x3c4a3a, roughness: 1 })
);
ground.rotation.x = -Math.PI / 2;
ground.receiveShadow = true;
scene.add(ground);

const plaza = new THREE.Mesh(
  new THREE.BoxGeometry(W + 16, 0.2, D + 16),
  new THREE.MeshStandardMaterial({ color: 0x9a9aa2, roughness: 0.9 })
);
plaza.position.y = 0.1;
plaza.receiveShadow = true;
scene.add(plaza);

// --- Building ---
const concrete = new THREE.MeshStandardMaterial({ color: 0xd8d8dc, roughness: 0.8 });
const glass = new THREE.MeshStandardMaterial({
  color: 0x6fb4e6, roughness: 0.08, metalness: 0.6, transparent: true, opacity: 0.55,
});
const frameMat = new THREE.MeshStandardMaterial({ color: 0x2a2f3a, roughness: 0.5, metalness: 0.4 });
const windowLit = new THREE.MeshStandardMaterial({
  color: 0x222222, emissive: 0xffd27a, emissiveIntensity: 0, transparent: true, opacity: 0.0,
});

const base = 0.2; // top of the plaza
const floors = []; // THREE.Group per floor
const building = new THREE.Group();
scene.add(building);

function box(w, h, d, mat, x, y, z) {
  const m = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), mat);
  m.position.set(x, y, z);
  m.castShadow = true;
  m.receiveShadow = true;
  return m;
}

for (let i = 0; i < FLOORS; i++) {
  const g = new THREE.Group();
  const y0 = base + i * FLOOR_H;

  // slab
  g.add(box(W, SLAB, D, concrete, 0, y0 + SLAB / 2, 0));

  // glass curtain wall (4 sides) with mullions
  const wallH = FLOOR_H - SLAB;
  const cy = y0 + SLAB + wallH / 2;
  g.add(box(W - 0.1, wallH, 0.08, glass, 0, cy, D / 2 - 0.05));
  g.add(box(W - 0.1, wallH, 0.08, glass, 0, cy, -D / 2 + 0.05));
  g.add(box(0.08, wallH, D - 0.1, glass, W / 2 - 0.05, cy, 0));
  g.add(box(0.08, wallH, D - 0.1, glass, -W / 2 + 0.05, cy, 0));

  const bays = 8;
  for (let b = 0; b <= bays; b++) {
    const x = -W / 2 + (b * W) / bays;
    g.add(box(0.2, wallH, 0.2, frameMat, x, cy, D / 2));
    g.add(box(0.2, wallH, 0.2, frameMat, x, cy, -D / 2));
  }
  for (let b = 1; b < 5; b++) {
    const z = -D / 2 + (b * D) / 5;
    g.add(box(0.2, wallH, 0.2, frameMat, W / 2, cy, z));
    g.add(box(0.2, wallH, 0.2, frameMat, -W / 2, cy, z));
  }

  // interior: core + a few partitions so the cutaway has something to show
  g.add(box(5, wallH, 4, concrete, 0, cy, 0));
  g.add(box(0.15, wallH * 0.9, 6, frameMat, -6, cy, -2));
  g.add(box(0.15, wallH * 0.9, 6, frameMat, 6, cy, 2));

  // lit-window glow panes (shown at night)
  const glow = new THREE.Mesh(new THREE.BoxGeometry(W - 0.5, wallH - 0.4, D - 0.5), windowLit.clone());
  glow.position.set(0, cy, 0);
  glow.userData.glow = true;
  g.add(glow);

  g.userData.index = i;
  floors.push(g);
  building.add(g);
}

// Roof
const roof = new THREE.Group();
const topY = base + FLOORS * FLOOR_H;
roof.add(box(W + 0.6, 0.4, D + 0.6, concrete, 0, topY + 0.2, 0));
roof.add(box(5.5, 2.2, 4.5, concrete, 0, topY + 1.5, 0));
const mast = box(0.2, 7, 0.2, frameMat, 0, topY + 5.2, 0);
roof.add(mast);
building.add(roof);

// Entrance canopy
const canopy = box(8, 0.3, 3, frameMat, 0, base + 3.2, D / 2 + 1.5);
building.add(canopy);
[-3.6, 3.6].forEach((x) => building.add(box(0.3, 3.2, 0.3, frameMat, x, base + 1.6, D / 2 + 2.8)));

// --- Surrounding trees ---
const trunkMat = new THREE.MeshStandardMaterial({ color: 0x5b4126 });
const leafMat = new THREE.MeshStandardMaterial({ color: 0x2f6f3a, roughness: 0.9 });
function tree(x, z, s = 1) {
  const t = new THREE.Group();
  const trunk = new THREE.Mesh(new THREE.CylinderGeometry(0.2 * s, 0.3 * s, 2 * s, 8), trunkMat);
  trunk.position.y = s;
  const leaves = new THREE.Mesh(new THREE.ConeGeometry(1.4 * s, 3.5 * s, 10), leafMat);
  leaves.position.y = 3.2 * s;
  [trunk, leaves].forEach((m) => { m.castShadow = true; t.add(m); });
  t.position.set(x, 0, z);
  scene.add(t);
}
for (let i = 0; i < 28; i++) {
  const a = (i / 28) * Math.PI * 2;
  const r = 24 + (i % 3) * 5;
  tree(Math.cos(a) * r * 1.2, Math.sin(a) * r, 0.8 + (i % 4) * 0.15);
}

// --- Sky / day-night ---
let night = false;
const dayBg = new THREE.Color(0x9fc9ee);
const nightBg = new THREE.Color(0x070b1a);
function applyTime() {
  scene.background = night ? nightBg : dayBg;
  scene.fog = new THREE.Fog(scene.background, 80, 220);
  sun.intensity = night ? 0.15 : 2.2;
  sun.color.set(night ? 0x8899ff : 0xffffff);
  hemi.intensity = night ? 0.15 : 0.6;
  floors.forEach((f, i) => {
    f.traverse((o) => {
      if (o.userData.glow) {
        const lit = night && (i * 7 + 3) % 5 !== 0; // a few dark floors for realism
        o.material.opacity = lit ? 0.35 : 0;
        o.material.emissiveIntensity = lit ? 1.2 : 0;
      }
    });
  });
}
applyTime();

// --- UI ---
const floorBox = document.getElementById('floors');
let isolated = -1;
const btns = [];

function setIsolated(idx) {
  isolated = idx;
  floors.forEach((f, i) => { f.visible = idx < 0 || i === idx; });
  roof.visible = idx < 0 ? roofOn : false;
  btns.forEach((b, i) => b.classList.toggle('active', i - 1 === idx));
  if (idx >= 0) {
    controls.target.set(0, base + idx * FLOOR_H + FLOOR_H / 2, 0);
  } else {
    controls.target.set(0, (FLOORS * FLOOR_H) / 2, 0);
  }
}

const allBtn = Object.assign(document.createElement('button'), { textContent: 'All' });
allBtn.onclick = () => setIsolated(-1);
floorBox.appendChild(allBtn);
btns.push(allBtn);
for (let i = 0; i < FLOORS; i++) {
  const b = Object.assign(document.createElement('button'), { textContent: String(i + 1) });
  b.onclick = () => setIsolated(i);
  floorBox.appendChild(b);
  btns.push(b);
}
btns[0].classList.add('active');

let roofOn = true;
document.getElementById('roof').onclick = () => {
  roofOn = !roofOn;
  if (isolated < 0) roof.visible = roofOn;
};
document.getElementById('daynight').onclick = () => { night = !night; applyTime(); };
const rotBtn = document.getElementById('rotate');
rotBtn.onclick = () => {
  controls.autoRotate = !controls.autoRotate;
  rotBtn.textContent = `Auto-rotate: ${controls.autoRotate ? 'on' : 'off'}`;
  rotBtn.classList.toggle('active', controls.autoRotate);
};
document.getElementById('reset').onclick = () => {
  camera.position.copy(HOME_CAM);
  setIsolated(-1);
};

// stop auto-rotate fighting the user while dragging
renderer.domElement.addEventListener('pointerdown', () => { controls.autoRotate = false; rotBtn.textContent = 'Auto-rotate: off'; rotBtn.classList.remove('active'); });

addEventListener('resize', () => {
  camera.aspect = innerWidth / innerHeight;
  camera.updateProjectionMatrix();
  renderer.setSize(innerWidth, innerHeight);
});

renderer.setAnimationLoop(() => {
  controls.update();
  renderer.render(scene, camera);
});

// exposed for quick debugging / tests
window.__building = { scene, camera, floors, setIsolated };
