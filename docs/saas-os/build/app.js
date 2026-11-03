(function(){
 const $=id=>document.getElementById(id);
 const f=n=>"₹"+Math.round(n).toLocaleString("en-IN");
 function run(){
  const arpa=+$("c_arpa").value,cp=+$("c_cp").value/100,fx=+$("c_fx").value,ch=+$("c_ch").value/100,cac=+$("c_cac").value,hrs=+$("c_h").value,rate=+$("c_r").value;
  const contrib=arpa*cp,be=Math.ceil(fx/contrib),life=1/ch,ltv=contrib*life;
  $("o_be").innerHTML=be+" tenants";$("o_ltv").innerHTML=f(ltv);$("o_ratio").innerHTML=(ltv/cac).toFixed(1)+" : 1";
  $("o_pay").innerHTML=(cac/contrib).toFixed(1)+" months";$("o_lab").innerHTML=f(hrs*rate);
  $("o_mrr").innerHTML=f(be*arpa)+" MRR at break-even";
 }
 document.querySelectorAll(".calc input").forEach(i=>i.addEventListener("input",run));run();
 const m=$("menu");if(m)m.addEventListener("click",()=>$("toc").classList.toggle("open"));
})();
