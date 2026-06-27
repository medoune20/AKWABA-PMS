// Thème clair/sombre persistant (mémoire de session, pas de stockage navigateur requis côté serveur)
(function(){
  const cle="akwaba-theme";
  const html=document.documentElement;
  try{ const t=localStorage.getItem(cle); if(t) html.setAttribute("data-theme",t); }catch(e){}
  window.basculerTheme=function(){
    const actuel=html.getAttribute("data-theme")==="sombre"?"clair":"sombre";
    html.setAttribute("data-theme",actuel);
    try{ localStorage.setItem(cle,actuel); }catch(e){}
  };
  window.basculerMenu=function(){
    document.querySelector(".barre")?.classList.toggle("ouvert");
    document.querySelector(".voile")?.classList.toggle("on");
  };
})();
