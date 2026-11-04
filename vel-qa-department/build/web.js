(function(){
  "use strict";
  var $=function(s,r){return (r||document).querySelector(s)},$$=function(s,r){return Array.prototype.slice.call((r||document).querySelectorAll(s))};
  var root=document.documentElement;
  try{var t=localStorage.getItem("vel-theme");if(t)root.setAttribute("data-theme",t);}catch(e){}
  function setTheme(v){root.setAttribute("data-theme",v);try{localStorage.setItem("vel-theme",v)}catch(e){}}
  $$("[data-act=theme]").forEach(function(b){b.addEventListener("click",function(){setTheme(root.getAttribute("data-theme")==="dark"?"light":"dark")})});
  $$("[data-act=print]").forEach(function(b){b.addEventListener("click",function(){window.print()})});
  $$("[data-act=menu]").forEach(function(b){b.addEventListener("click",function(){document.body.classList.toggle("nav-open")})});
  document.addEventListener("click",function(e){if(document.body.classList.contains("nav-open")&&!e.target.closest(".sidebar")&&!e.target.closest("[data-act=menu]"))document.body.classList.remove("nav-open")});

  /* ---- index: every h1/h2 block becomes a searchable record ---- */
  var doc=$(".doc");if(!doc)return;
  var heads=$$("h1[id],h2[id]",doc),recs=[],chapter="";
  heads.forEach(function(h,i){
    if(h.tagName==="H1")chapter=h.textContent.replace(/^\s*\d+\.\s*/,"");
    var txt=[],n=h.tagName==="H1"?h:h;                 // collect text until next h1/h2
    var cur=h.tagName==="H1"?h.nextElementSibling:h.nextElementSibling;
    var stop=heads[i+1];
    var node=h.tagName==="H1"?h.nextSibling:h.nextSibling;
    while(node&&node!==stop){
      if(node.nodeType===1){ if(node.querySelector&&stop&&node.contains(stop))break; txt.push(node.textContent)}
      else if(node.nodeType===3)txt.push(node.textContent);
      node=node.nextSibling;
    }
    recs.push({id:h.id,title:h.textContent.replace(/^\s*\d+\.\s*/,""),chapter:chapter,text:txt.join(" ").replace(/\s+/g," "),h:h.tagName});
  });

  var input=$("#q"),res=$("#results"),nav=$("#navlist");
  function esc(s){return s.replace(/[&<>"]/g,function(c){return{"&":"&amp;","<":"&lt;",">":"&gt;",'"':"&quot;"}[c]})}
  function rx(t){return new RegExp("("+t.map(function(x){return x.replace(/[.*+?^${}()|[\]\\]/g,"\\$&")}).join("|")+")","gi")}
  var timer;
  function run(){
    var q=input.value.trim().toLowerCase();clearHits();
    if(q.length<2){res.innerHTML="";nav.hidden=false;return}
    var terms=q.split(/\s+/).filter(Boolean),out=[];
    recs.forEach(function(r){
      var hay=(r.title+" "+r.text).toLowerCase(),ok=terms.every(function(t){return hay.indexOf(t)>-1});
      if(!ok)return;
      var s=0;terms.forEach(function(t){if(r.title.toLowerCase().indexOf(t)>-1)s+=10;var m=hay.split(t).length-1;s+=Math.min(m,8)});
      out.push({r:r,s:s});
    });
    out.sort(function(a,b){return b.s-a.s});nav.hidden=true;
    if(!out.length){res.innerHTML='<li style="padding:8px;color:var(--muted)">No matches for “'+esc(q)+'”.</li>';return}
    var re=rx(terms);
    res.innerHTML=out.slice(0,40).map(function(o){
      var r=o.r,low=r.text.toLowerCase(),at=low.indexOf(terms[0]);if(at<0)at=0;
      var snip=r.text.substr(Math.max(0,at-50),150);
      return '<li><a href="#'+r.id+'" data-q="'+esc(q)+'"><span class="rc">'+esc(r.chapter)+'</span><span class="rt">'+esc(r.title).replace(re,"<mark>$1</mark>")+'</span><span class="rs">…'+esc(snip).replace(re,"<mark>$1</mark>")+'…</span></a></li>';
    }).join("")+(out.length>40?'<li style="padding:6px;color:var(--muted);font-size:12px">'+(out.length-40)+' more — refine your search.</li>':"");
  }
  function clearHits(){$$(".hit").forEach(function(m){var p=m.parentNode;p.replaceChild(document.createTextNode(m.textContent),m);p.normalize()})}
  function highlight(id,q){
    clearHits();var h=document.getElementById(id);if(!h)return;var terms=q.split(/\s+/).filter(Boolean),re=rx(terms);
    var rec=recs.filter(function(r){return r.id===id})[0],i=heads.indexOf(h),stop=heads[i+1],n=0;
    var w=document.createTreeWalker(doc,NodeFilter.SHOW_TEXT,null),list=[],node;
    while((node=w.nextNode())){
      if(!(h.compareDocumentPosition(node)&Node.DOCUMENT_POSITION_FOLLOWING)&&node.parentNode!==h&&!h.contains(node))continue;
      if(stop&&(stop.compareDocumentPosition(node)&Node.DOCUMENT_POSITION_FOLLOWING||stop.contains(node)))break;
      if(node.parentNode.closest("script,style"))continue;
      re.lastIndex=0;if(re.test(node.nodeValue))list.push(node);
    }
    list.slice(0,60).forEach(function(tn){
      var frag=document.createDocumentFragment(),parts=tn.nodeValue.split(re);
      parts.forEach(function(s,k){if(!s)return;if(k%2){var m=document.createElement("mark");m.className="hit";m.textContent=s;frag.appendChild(m)}else frag.appendChild(document.createTextNode(s))});
      tn.parentNode.replaceChild(frag,tn);n++;
    });
  }
  if(input){
    input.addEventListener("input",function(){clearTimeout(timer);timer=setTimeout(run,120)});
    input.addEventListener("keydown",function(e){if(e.key==="Escape"){input.value="";run();input.blur()}if(e.key==="Enter"){var a=$("a",res);if(a)a.click()}});
    res.addEventListener("click",function(e){var a=e.target.closest("a");if(!a)return;var q=a.getAttribute("data-q"),id=a.getAttribute("href").slice(1);
      setTimeout(function(){highlight(id,q);var h=document.getElementById(id);if(h)h.scrollIntoView({block:"start"})},30);document.body.classList.remove("nav-open")});
    document.addEventListener("keydown",function(e){
      var tag=(e.target.tagName||"").toLowerCase();
      if((e.key==="/"&&tag!=="input"&&tag!=="textarea")||((e.ctrlKey||e.metaKey)&&e.key.toLowerCase()==="k")){e.preventDefault();document.body.classList.add("nav-open");input.focus();input.select()}
    });
  }

  /* ---- nav highlighting + progress + back-to-top ---- */
  var links={};$$("a[href^='#']",nav).forEach(function(a){links[a.getAttribute("href").slice(1)]=a});
  var fab=$(".fab"),bar=$(".progress");
  function onScroll(){
    var y=window.scrollY,h=document.documentElement.scrollHeight-window.innerHeight;
    if(bar)bar.style.width=(h>0?100*y/h:0)+"%";if(fab)fab.classList.toggle("show",y>800);
    var cur=null;for(var i=0;i<heads.length;i++){if(heads[i].getBoundingClientRect().top<140)cur=heads[i];else break}
    $$("a.on",nav).forEach(function(a){a.classList.remove("on")});$$("li.open",nav).forEach(function(l){l.classList.remove("open")});
    if(cur){var a=links[cur.id];if(a){a.classList.add("on");var li=a.closest("li");while(li){li.classList.add("open");li=li.parentNode.closest("li")}}}
  }
  var tick=false;window.addEventListener("scroll",function(){if(!tick){tick=true;requestAnimationFrame(function(){onScroll();tick=false})}},{passive:true});
  if(fab)fab.addEventListener("click",function(){window.scrollTo({top:0})});
  $$("a[href^='#']",nav).forEach(function(a){a.addEventListener("click",function(){document.body.classList.remove("nav-open")})});
  onScroll();
  window.addEventListener("beforeprint",clearHits);
})();
