(function(){
var d=document,$=function(s){return d.querySelector(s)};
function sg(k,v){try{if(v===undefined)return localStorage.getItem(k);localStorage.setItem(k,v)}catch(e){return null}}
var root=d.documentElement,th=sg('vel-theme');
if(!th&&window.matchMedia&&matchMedia('(prefers-color-scheme: dark)').matches)th='dark';
if(th)root.setAttribute('data-theme',th);
$('#theme').onclick=function(){var n=root.getAttribute('data-theme')==='dark'?'light':'dark';root.setAttribute('data-theme',n);sg('vel-theme',n)};
$('#print').onclick=function(){root.setAttribute('data-theme','light');window.print()};
var side=$('#side'),menu=$('#menu');
menu.onclick=function(){var o=side.classList.toggle('open');menu.setAttribute('aria-expanded',o)};
side.addEventListener('click',function(e){if(e.target.tagName==='A')side.classList.remove('open')});
// scroll spy
var links={};side.querySelectorAll('a[data-id]').forEach(function(a){links[a.dataset.id]=a});
if('IntersectionObserver' in window){var io=new IntersectionObserver(function(es){es.forEach(function(e){if(e.isIntersecting){Object.values(links).forEach(function(l){l.classList.remove('on')});var l=links[e.target.id];if(l){l.classList.add('on');l.scrollIntoView({block:'nearest'})}}})},{rootMargin:'-20% 0px -70% 0px'});
d.querySelectorAll('section.chapter').forEach(function(s){io.observe(s)})}
// copy buttons
d.querySelectorAll('.copy').forEach(function(b){b.onclick=function(){var t=b.parentNode.querySelector('code').innerText;(navigator.clipboard?navigator.clipboard.writeText(t):Promise.reject()).then(function(){b.textContent='Copied'},function(){b.textContent='Select + Ctrl+C'});setTimeout(function(){b.textContent='Copy'},1500)}});
// back to top
var tp=$('#top');window.addEventListener('scroll',function(){tp.style.display=scrollY>600?'block':'none'},{passive:true});tp.onclick=function(){scrollTo({top:0})};
// search index
var idx=[];
d.querySelectorAll('section.chapter').forEach(function(sec){var h1=sec.querySelector('h1'),cur=h1.textContent,curEl=h1;
 sec.querySelectorAll('h1,h2,h3,p,li,td,th,figcaption').forEach(function(el){if(/^H[123]$/.test(el.tagName)){cur=el.textContent;curEl=el}
  var t=el.textContent.replace(/\s+/g,' ').trim();if(t.length>2)idx.push({el:el,sec:h1.textContent,head:cur,t:t,lc:t.toLowerCase()})})});
var q=$('#q'),res=$('#res'),tm;
function esc(s){return s.replace(/[&<>]/g,function(c){return{'&':'&amp;','<':'&lt;','>':'&gt;'}[c]})}
function run(){var v=q.value.trim().toLowerCase();if(v.length<2){res.hidden=true;return}
 var terms=v.split(/\s+/),hits=[];for(var i=0;i<idx.length&&hits.length<25;i++){var r=idx[i],ok=true;for(var j=0;j<terms.length;j++)if(r.lc.indexOf(terms[j])<0){ok=false;break}if(ok)hits.push(r)}
 if(!hits.length){res.innerHTML='<div class="none">No results for “'+esc(v)+'”</div>';res.hidden=false;return}
 res.innerHTML='';hits.forEach(function(h){var p=h.lc.indexOf(terms[0]),s=Math.max(0,p-50),sn=h.t.substr(s,140);var a=d.createElement('a');a.href='#';
  var html=esc(sn);terms.forEach(function(t){html=html.replace(new RegExp(t.replace(/[.*+?^${}()|[\]\\]/g,'\\$&'),'gi'),function(m){return'<mark>'+m+'</mark>'})});
  a.innerHTML='<b>'+esc(h.sec)+' › '+esc(h.head).slice(0,60)+'</b>'+(s>0?'…':'')+html+'…';
  a.onclick=function(e){e.preventDefault();res.hidden=true;h.el.scrollIntoView({behavior:'smooth',block:'center'});h.el.classList.add('flash');setTimeout(function(){h.el.classList.remove('flash')},1700)};res.appendChild(a)});res.hidden=false}
q.addEventListener('input',function(){clearTimeout(tm);tm=setTimeout(run,120)});
q.addEventListener('keydown',function(e){if(e.key==='Escape'){res.hidden=true;q.blur()}});
d.addEventListener('keydown',function(e){if(e.key==='/'&&d.activeElement!==q&&!/INPUT|TEXTAREA/.test(d.activeElement.tagName)){e.preventDefault();q.focus()}});
d.addEventListener('click',function(e){if(!e.target.closest('.sw'))res.hidden=true});
})();
