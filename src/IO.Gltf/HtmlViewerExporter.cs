using System.Net;

namespace BuildingStudio.IO.Gltf;

/// <summary>
/// Self-contained HTML viewer for sharing a walkthrough with clients: the GLB is embedded as base64 and
/// three.js is loaded from a CDN (needs internet when opened).
/// </summary>
public static class HtmlViewerExporter
{
    public static string Build(byte[] glb, string title, string subtitle)
    {
        var b64 = Convert.ToBase64String(glb);
        var t = WebUtility.HtmlEncode(title);
        var s = WebUtility.HtmlEncode(subtitle);
        return $$$"""
<!doctype html>
<html lang="en">
<head>
<meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<title>{{{t}}}</title>
<style>
html,body{margin:0;height:100%;overflow:hidden;background:#dfe9f3;font-family:system-ui,Segoe UI,Arial,sans-serif}
#hud{position:fixed;left:16px;top:16px;max-width:320px;background:rgba(255,255,255,.92);padding:12px 14px;border-radius:10px;box-shadow:0 2px 10px rgba(0,0,0,.15)}
#hud h1{font-size:16px;margin:0 0 4px} #hud p{font-size:12px;margin:0 0 8px;color:#444}
button{border:0;border-radius:6px;background:#2c3e50;color:#fff;padding:7px 10px;margin:2px 2px 0 0;cursor:pointer;font-size:12px}
button.on{background:#c0392b}
</style>
<script type="importmap">{"imports":{"three":"https://unpkg.com/three@0.160.0/build/three.module.js","three/addons/":"https://unpkg.com/three@0.160.0/examples/jsm/"}}</script>
</head>
<body>
<div id="hud"><h1>{{{t}}}</h1><p>{{{s}}}</p>
<button id="tour">Walk-around tour</button><button id="day" class="on">Day</button><button id="reset">Reset view</button>
<p style="margin-top:8px">Drag to orbit · scroll to zoom · right-drag to pan</p></div>
<script type="module">
import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';
import { GLTFLoader } from 'three/addons/loaders/GLTFLoader.js';
const scene = new THREE.Scene(); scene.background = new THREE.Color(0xdfe9f3);
const camera = new THREE.PerspectiveCamera(45, innerWidth/innerHeight, 0.1, 1000);
const renderer = new THREE.WebGLRenderer({antialias:true}); renderer.setPixelRatio(Math.min(devicePixelRatio,2));
renderer.setSize(innerWidth, innerHeight); renderer.shadowMap.enabled = true; document.body.appendChild(renderer.domElement);
const hemi = new THREE.HemisphereLight(0xffffff, 0x8899aa, 1.1); scene.add(hemi);
const sun = new THREE.DirectionalLight(0xffffff, 1.6); sun.position.set(20, 30, 15); sun.castShadow = true; sun.shadow.bias = -0.0004; sun.shadow.normalBias = 0.03; sun.shadow.mapSize.set(2048, 2048); scene.add(sun);
const controls = new OrbitControls(camera, renderer.domElement); controls.enableDamping = true; controls.maxPolarAngle = Math.PI/2 - 0.03;
const data = Uint8Array.from(atob('{{{b64}}}'), c => c.charCodeAt(0)).buffer;
let center = new THREE.Vector3(), radius = 10, home;
new GLTFLoader().parse(data, '', g => {
  g.scene.traverse(o => { if (o.isMesh) { o.castShadow = true; o.receiveShadow = true; } });
  scene.add(g.scene);
  const box = new THREE.Box3().setFromObject(g.scene); box.getCenter(center); radius = box.getSize(new THREE.Vector3()).length()/2;
  const sc = sun.shadow.camera; sc.left = sc.bottom = -radius*1.5; sc.right = sc.top = radius*1.5; sc.far = radius*6; sc.updateProjectionMatrix(); sun.position.copy(center).add(new THREE.Vector3(radius, radius*1.6, radius*0.8)); sun.target.position.copy(center); scene.add(sun.target);
  controls.target.copy(center); home = center.clone().add(new THREE.Vector3(radius*0.9, radius*0.6, radius*0.9));
  camera.position.copy(home);
});
let touring = false, t0 = 0;
document.getElementById('tour').onclick = e => { touring = !touring; t0 = performance.now(); e.target.classList.toggle('on', touring); };
document.getElementById('reset').onclick = () => { touring = false; camera.position.copy(home); controls.target.copy(center); };
let day = true;
document.getElementById('day').onclick = e => { day = !day; e.target.textContent = day ? 'Day' : 'Night';
  scene.background = new THREE.Color(day ? 0xdfe9f3 : 0x0d1424); hemi.intensity = day ? 1.1 : 0.25; sun.intensity = day ? 1.6 : 0.1; };
addEventListener('resize', () => { camera.aspect = innerWidth/innerHeight; camera.updateProjectionMatrix(); renderer.setSize(innerWidth, innerHeight); });
renderer.setAnimationLoop(now => {
  if (touring) { const a = (now - t0)/9000*Math.PI*2; camera.position.set(center.x + Math.cos(a)*radius*1.2, center.y + radius*0.35, center.z + Math.sin(a)*radius*1.2); controls.target.copy(center); }
  controls.update(); renderer.render(scene, camera);
});
</script>
</body>
</html>
""";
    }
}
