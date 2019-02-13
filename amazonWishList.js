// verifica o valor da lista de desejos no site da amazon | console  var list = $$('.a-price'); var 
total=0;  for (var i = 0; i < list.length; i++) { 	total += 
parseFloat(list[i].innerText.split("R$")[1].replace(',','.')) ; }  console.log("Total = " + total);
