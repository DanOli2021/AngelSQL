async function login(user, password) {
  return sendToAngelPOST(user, "tokens/admintokens", "", "GetTokenFromUser", { User: user, Password: password });
}

async function GetGroupsUsingTocken(user, token) {
  return sendToAngelPOST(user, "tokens/admintokens", "", "GetGroupsUsingTocken", { TokenToObtainPermission: token });
}

async function GetWaitersTables(user, token) {
    return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "GetWaitersTables", {});  
}

async function GetWaiters(user, token) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "GetWaiters", {});  
}

async function GetTablesFromWaiter(user, token, id) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "GetTablesFromWaiter", { Id:id });  
}

async function SaveWaitersTables(user, token, waiter) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "SaveWaitersTables", waiter);  
}

async function GetWaiterTables(user, token, waiter_id) {

  var waiter = "";

  if (waiter_id.includes("@")) {
    waiter = waiter_id.split("@")[0];
  }

  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "GetWaiterTables", { Waiter: waiter } );  

}

async function DeleteWaitersTables(user, token, id) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "DeleteWaitersTables", { Id: id});  
}

async function GetTableCommands(user, token, table) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "GetTableCommands", { Table: table});  
}

async function SaveOrderPreference(user, token, order) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "SaveOrderPreference", order);  
}

async function GetIdOrder(user, token) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "GetIdOrder", {});  
}

async function SaveTableOrder(user, token, order) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "SaveTableOrder", order);  
}

async function DeleteTableOrder(user, token, order) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "DeleteTableOrder", order);  
}

async function GetClassifications(user, token) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "GetClassifications", {});  
}

async function GetSubClassifications(user, token, clasification) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "GetSubClassifications", { Clasification: clasification});  
}

async function GetMenuClassifications(user, token, subclasification) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "GetMenuClassifications", { SubClasification: subclasification});  
}

async function GetMenuOptions(user, token, nplatillo) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "GetMenuOptions", { nPlatillo: nplatillo});  
}

async function ConfirmOrderForPrinting(user, token, table) {
  return sendToAngelPOST(user, "RestaurantCommander/restaurantcommander", token, "ConfirmOrderForPrinting", { Table: table});  
}

async function sendToAngelPOST(user, api_name, token, OperationType, object_data) {

  account = "";

  if (user.includes("@")) {
    account = user.split("@")[1];
  }

  var api = {
    api: api_name,
    account: account,
    language: "C#",
    message:
    {
      OperationType: OperationType,
      Token: token,
      User: user,
      DataMessage: object_data,
      UserLanguage: getSelectedLanguage()
    }
  };

  return await sendPOST(api);

}


async function SendFile(user, api_name, token, OperationType, file, dataMessage) {

  account = "";

  if (user.includes("@")) {
    account = user.split("@")[1];
  }

  var api = {
    api: api_name,
    account: account,
    language: "C#",
    message:
    {
      OperationType: OperationType,
      Token: token,
      User: user,
      DataMessage: dataMessage,
      UserLanguage: getSelectedLanguage()
    }
  };
  
  const formData = new FormData();
  formData.append('file', file);
  formData.append('jSonString', JSON.stringify(api));

  url = window.location.protocol + '//' + window.location.host + "/AngelUpload";

  const response = await fetch(url, {
    method: 'POST',
    body: formData
  });

  if (!response.ok) {
    throw new Error(`Error HTTP: ${response.status}`);
  }

  const result = await response.text();
  return result;

}


async function sendPOST(data) {

  url = window.location.protocol + '//' + window.location.host + "/AngelPOST";

  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
  });

  if (!response.ok) {
    throw new Error(`Error HTTP: ${response.status}`);
  }

  const result = await response.text();
  return result;
}


function generateButton(href, iconSrc, buttonText, buttonClass, onclick = {}, background_color = "") {
  let button = document.createElement("a");
  button.href = href;
  button.className = buttonClass;  
  button.style.paddingRight = "120px";  

  if( background_color != "" ) 
  {
    button.style.backgroundColor = background_color;
  }  

  let iconSpan = document.createElement("span");
  iconSpan.className = "material-symbols-outlined";
  iconSpan.style.float = "left";

  let iconImg = document.createElement("img");
  iconImg.src = "images/icons/" + iconSrc;
  iconImg.style.width = "96px";

  let text = document.createElement("h2");
  text.innerText = translate_element(getSelectedLanguage(), buttonText);

  iconSpan.appendChild(iconImg);
  button.appendChild(iconSpan);
  button.appendChild(text);

  button.onclick = onclick;

  let div = document.getElementById("buttonszone");
  div.appendChild(button);

}


function generateOrder(href, iconSrc, buttonText, buttonClass, onclick = {}, background_color = "", style = "") {
  let button = document.createElement("button");
  button.href = href;
  button.className = buttonClass;  
  button.style.paddingRight = "120px";  

  if( background_color != "" ) 
  {
    button.style.backgroundColor = background_color;
  }  

  let iconSpan = document.createElement("span");
  iconSpan.className = "material-symbols-outlined";
  iconSpan.style.float = "left";

  let iconImg = document.createElement("img");
  iconImg.src = "images/" + iconSrc;

  if( style != "" ) 
  {
    iconImg.style = style;
  } 
  else 
  {
    iconImg.style.width = "120px";
  }
  
  let text = document.createElement("h2");
  text.innerText = translate_element(getSelectedLanguage(), buttonText);

  iconSpan.appendChild(iconImg);
  button.appendChild(iconSpan);
  button.appendChild(text);

  button.onclick = onclick;

  let div = document.getElementById("buttonszone");
  div.appendChild(button);

}



function formatearComoMoneda(numero, codigoMoneda = 'USD', locale = 'en-US') {
    return new Intl.NumberFormat(locale, {
        style: 'currency',
        currency: codigoMoneda,
    }).format(numero);
}


function generateParagraph(element, text, classstring, stylestring) {
  let p = document.createElement(element);
  p.innerText = translate_element(getSelectedLanguage(), text);
  p.style.textAlign = stylestring;
  p.className = classstring;
  let div = document.getElementById("buttonszone");
  div.appendChild(p);
}


function showDialog(title, message) {
  document.getElementById('dialogTittle').innerText = title;
  document.getElementById('dialogMessage').innerText = '⚠️' + message;
  document.getElementById('myDialog').showModal();
}

function closeDialog() {
  document.getElementById('myDialog').close();
}

function showAcceptCancelDialog(title, message, operation) {
  document.getElementById('dialogTittleAcceptCancel').innerText = translate_element("es", title);
  document.getElementById('dialogMessageAcceptCancel').innerText = translate_element("es", message);
  document.getElementById('DialogAcceptCancel').showModal();
  document.getElementById('cancel').focus();

  var accept = document.getElementById('accept');
  accept.focus();
  accept.onclick = operation;
}

function closeDialogAcceptCancelDialog() {
  document.getElementById('DialogAcceptCancel').close();
}

// Función para verificar si la URL es una imagen válida
function isValidImageUrl(url) {
  // Expresión regular para verificar la extensión de la imagen
  var image_extensions = /\.(jpeg|jpg|gif|png)$/i;

  // Verifica si la URL tiene una extensión de imagen válida
  if (image_extensions.test(url)) {
    return true;
  }

  return false;
}


function isValidDateFormat(dateStr) {
  const regex = /^\d{4}-\d{2}-\d{2}$/;

  // Verifica si cumple con el formato
  if (!regex.test(dateStr)) {
    return false;
  }

  // Trata de crear un objeto Date con la fecha
  const dateObj = new Date(dateStr);

  // Verifica que sea una fecha válida (esto evita fechas como 2023-02-30, por ejemplo)
  if (dateObj.toISOString().slice(0, 10) !== dateStr) {
    return false;
  }

  return true;
}

function parseDate(input) {
  var parts = input.split(' ');
  var dateParts = parts[0].split('-');
  var timeParts = parts[1].split(':');
  // new Date(year, month [, day [, hours[, minutes[, seconds[, ms]]]]])
  return new Date(dateParts[0], dateParts[1] - 1, dateParts[2], timeParts[0], timeParts[1], timeParts[2]);
}


function saveSelectedLanguage(language) {
  var date = new Date();
  date.setTime(date.getTime() + (30 * 24 * 60 * 60 * 1000)); // 30 days from now
  var expires = "expires=" + date.toUTCString();
  document.cookie = "language=" + language + "; " + expires + "; path=/";
}


function SaveLanguage(language) {
  saveSelectedLanguage(language);
  window.location.reload();
}


function getSelectedLanguage() {
  var name = "language=";
  var decodedCookie = decodeURIComponent(document.cookie);
  var ca = decodedCookie.split(';');
  for (var i = 0; i < ca.length; i++) {
    var c = ca[i];
    while (c.charAt(0) === ' ') {
      c = c.substring(1);
    }
    if (c.indexOf(name) === 0) {
      return c.substring(name.length, c.length);
    }
  }
  return "";
}



//Busca una cadena dentro de otra, cuando la primera esta separada por comas
function findInString(stringToSearch, stringToFind) {
  var array = stringToFind.split(",");
  var find = false;

  for (var i = 0; i < array.length; i++) {
    if (stringToSearch.includes(array[i])) {
      find = true;
      break;
    }
  }

  return find;
}

//Botons clasifiacion
function clasifications_buttons(href, iconSrc, buttonText, buttonClass, onclick = {}, background_color = "", style = "") {
  let button = document.createElement("button");

  button.href = href;
  button.className = buttonClass;  
  button.style.color = "white";

  if( background_color != "" ) 
  {
    button.style.backgroundColor = background_color;
  }  
  
  let text = document.createElement("H5");
  text.innerText = translate_element(getSelectedLanguage(), buttonText);

  button.appendChild(text);
  button.onclick = onclick;

  let div = document.getElementById("clasifications_buttons");
  div.appendChild(button);

}

function subclasifications_buttons(href, iconSrc, buttonText, buttonClass, onclick = {}, background_color = "", style = "") {
  let button = document.createElement("button");
  
  button.href = href;
  button.className = buttonClass;  
  button.style.color = "white";

  if( background_color != "" ) 
  {
    button.style.backgroundColor = background_color;
  }  

  let text = document.createElement("H5");
  text.innerText = translate_element(getSelectedLanguage(), buttonText);

  button.appendChild(text);
  button.onclick = onclick;

  let div = document.getElementById("subclasifications_buttons");
  div.appendChild(button);

}


function menu_buttons(href, iconSrc, buttonText, buttonClass, onclick = {}, background_color = "", style = "") {
  let button = document.createElement("button");

  button.href = href;
  button.className = buttonClass;  
  button.style.color = "white";

  if( background_color != "" ) 
  {
    button.style.backgroundColor = background_color;
  }  
  
  let text = document.createElement("H5");
  text.innerText = translate_element(getSelectedLanguage(), buttonText);

  button.appendChild(text);
  button.onclick = onclick;

  let div = document.getElementById("menu_buttons");
  div.appendChild(button);

}

function options_buttons(colummnButton, href, iconSrc, buttonText, buttonClass, onclick = {}, background_color = "", style = "") {
  let button = document.createElement("button");

  button.href = href;
  button.className = buttonClass;  
  button.style.color = "white";

  if( background_color != "" ) 
  {
    button.style.backgroundColor = background_color;
  }  
  
  let text = document.createElement("H6");
  text.innerText = translate_element(getSelectedLanguage(), buttonText);

  button.appendChild(text);
  button.onclick = onclick;

  let div = document.getElementById("options_buttons" + colummnButton);
  div.appendChild(button);

}