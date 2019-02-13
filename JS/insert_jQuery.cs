//insere jquery em qualquer página | console

var jq = document.createElement('script');
jq.src = "//cdnjs.cloudflare.com/ajax/libs/jquery/2.1.1/jquery.min.js";
document.getElementsByTagName('head')[0].appendChild(jq);
// ... dar um tempinho pro script carregar
setTimeout( function(){
    jQuery.noConflict();
    $=jQuery;
    console.log('Carregado jQuery v' + $.fn.jquery);
}, 3000);
