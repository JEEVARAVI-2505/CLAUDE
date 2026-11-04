(function(){
  var $=function(s,c){return (c||document).querySelector(s)},$$=function(s,c){return Array.prototype.slice.call((c||document).querySelectorAll(s))};
  // copy buttons
  $$('pre').forEach(function(pre){
    if(pre.closest('.nocopy'))return;
    var b=document.createElement('button');b.className='copybtn no-print';b.type='button';b.textContent='Copy';b.setAttribute('aria-label','Copy code');
    b.addEventListener('click',function(){
      var t=pre.querySelector('code').innerText;
      var done=function(){b.textContent='Copied';setTimeout(function(){b.textContent='Copy'},1400)};
      if(navigator.clipboard&&window.isSecureContext){navigator.clipboard.writeText(t).then(done)}
      else{var ta=document.createElement('textarea');ta.value=t;document.body.appendChild(ta);ta.select();try{document.execCommand('copy');done()}catch(e){}document.body.removeChild(ta)}
    });pre.appendChild(b);
  });
  // module completion (best-effort localStorage)
  var store={get:function(k){try{return localStorage.getItem(k)}catch(e){return null}},set:function(k,v){try{localStorage.setItem(k,v)}catch(e){}}};
  $$('input[data-done]').forEach(function(c){
    var k='vel-wd-done:'+c.dataset.done;c.checked=store.get(k)==='1';
    c.addEventListener('change',function(){store.set(k,c.checked?'1':'0');updateProgress()});
  });
  function updateProgress(){var all=$$('input[data-done]'),n=all.filter(function(c){return c.checked}).length,p=$('#progress');if(p)p.textContent='Modules done: '+n+' / '+all.length}
  updateProgress();
  // expand / collapse all
  var setAll=function(o){$$('details.module').forEach(function(d){d.open=o})};
  var ex=$('#expand'),co=$('#collapse');if(ex)ex.onclick=function(){setAll(true)};if(co)co.onclick=function(){setAll(false)};
  window.addEventListener('beforeprint',function(){setAll(true)});
  // mobile nav
  var mb=$('#menu');if(mb)mb.onclick=function(){document.body.classList.toggle('navopen')};
  $$('.sidebar a').forEach(function(a){a.addEventListener('click',function(){document.body.classList.remove('navopen');
    var t=document.getElementById(a.getAttribute('href').slice(1));if(t){var d=t.closest('details.module')||(t.matches('details.module')?t:null);if(d)d.open=true}})});
  // scroll spy
  var links={};$$('.sidebar a[href^="#"]').forEach(function(a){links[a.getAttribute('href').slice(1)]=a});
  var heads=$$('section.sec[id],section.sec h2[id],details.module[id]');
  var io=new IntersectionObserver(function(es){es.forEach(function(e){if(e.isIntersecting){
    $$('.sidebar a.active').forEach(function(a){a.classList.remove('active')});
    var a=links[e.target.id];if(a){a.classList.add('active');var g=a.closest('.grp');$$('.grp.open').forEach(function(x){if(x!==g)x.classList.remove('open')});if(g)g.classList.add('open');
      var sb=$('.sidebar'),r=a.getBoundingClientRect();if(r.top<80||r.bottom>innerHeight-40)a.scrollIntoView({block:'center'})}}})},{rootMargin:'-70px 0px -75% 0px'});
  heads.forEach(function(h){io.observe(h)});
  // search
  var q=$('#q'),res=$('#results'),units=[];
  $$('section.sec').forEach(function(s){
    var title=s.querySelector('h1').textContent.trim();
    var blocks=[{el:s,title:title,part:'',text:s.textContent}];
    $$('details.module',s).forEach(function(m){blocks.push({el:m,title:m.querySelector('h3').textContent.trim(),part:title})});
    $$('h2',s).forEach(function(h){blocks.push({el:h,title:h.textContent.trim(),part:title,h2:true})});
    blocks.forEach(function(b){if(b.h2){var t='',n=b.el.nextElementSibling;while(n&&!/^H[12]$/.test(n.tagName)&&n.tagName!=='DETAILS'){t+=' '+n.textContent;n=n.nextElementSibling}b.text=b.title+' '+t}
      else if(b.el.matches('details')){b.text=b.el.textContent}else{b.text=''}});
    units=units.concat(blocks.filter(function(b){return b.text}));
  });
  function clearMarks(){$$('mark.hit').forEach(function(m){var p=m.parentNode;p.replaceChild(document.createTextNode(m.textContent),m);p.normalize()})}
  function esc(s){return s.replace(/[&<>"]/g,function(c){return {'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c]})}
  var timer;
  if(q)q.addEventListener('input',function(){clearTimeout(timer);timer=setTimeout(run,120)});
  function run(){
    var v=q.value.trim().toLowerCase();clearMarks();
    if(v.length<2){res.style.display='none';return}
    var hits=[];units.forEach(function(u){var t=u.text.toLowerCase(),i=t.indexOf(v);if(i>-1){
      var s=u.text.substr(Math.max(0,i-50),150).replace(/\s+/g,' ');hits.push({u:u,s:s,score:(u.title.toLowerCase().indexOf(v)>-1?0:1)})}});
    hits.sort(function(a,b){return a.score-b.score});
    res.innerHTML=hits.length?hits.slice(0,25).map(function(h,i){return '<a href="#" data-i="'+i+'"><b>'+esc(h.u.title)+'</b><small>'+(h.u.part?esc(h.u.part)+' — ':'')+'…'+esc(h.s)+'…</small></a>'}).join(''):'<div class="none">No matches for “'+esc(v)+'”.</div>';
    res.style.display='block';
    $$('a',res).forEach(function(a){a.onclick=function(e){e.preventDefault();var h=hits[+a.dataset.i];res.style.display='none';
      var d=h.u.el.closest('details.module');if(d)d.open=true;h.u.el.scrollIntoView({behavior:'smooth',block:'start'});highlight(h.u.el.matches('h2')?h.u.el.parentNode:h.u.el,v)}});
  }
  function highlight(root,v){
    var w=document.createTreeWalker(root,NodeFilter.SHOW_TEXT,null),nodes=[],n;
    while((n=w.nextNode())){if(n.parentNode.closest('script,style,.copybtn,mark'))continue;if(n.nodeValue.toLowerCase().indexOf(v)>-1)nodes.push(n)}
    nodes.slice(0,60).forEach(function(n){var t=n.nodeValue,l=t.toLowerCase(),i=l.indexOf(v),f=document.createDocumentFragment(),last=0;
      while(i>-1){f.appendChild(document.createTextNode(t.slice(last,i)));var m=document.createElement('mark');m.className='hit';m.textContent=t.substr(i,v.length);f.appendChild(m);last=i+v.length;i=l.indexOf(v,last)}
      f.appendChild(document.createTextNode(t.slice(last)));n.parentNode.replaceChild(f,n)});
  }
  document.addEventListener('click',function(e){if(res&&!res.contains(e.target)&&e.target!==q)res.style.display='none'});
  document.addEventListener('keydown',function(e){if(e.key==='/'&&document.activeElement.tagName!=='INPUT'){e.preventDefault();q&&q.focus()}if(e.key==='Escape'){res.style.display='none';q&&q.blur()}});
})();
