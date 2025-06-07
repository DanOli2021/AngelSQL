async function login(user, password) {
  return sendToAngelPOST(user, "tokens/admintokens", "", "GetTokenFromUser", { User: user, Password: password });
}

async function GetGroupsUsingTocken(user, token) {
  return sendToAngelPOST(user, "tokens/admintokens", "", "GetGroupsUsingTocken", { TokenToObtainPermission: token });
}

async function GetSalePerMonth(user, token, InitialDate, FinalDate) {
  return sendToAngelPOST(user, "pos_backend/pos_analysis", token, "SalesPerMonth", { StartDate: InitialDate, EndDate: FinalDate });
}

async function SalesByClassification(user, token, InitialDate, FinalDate) {
    return sendToAngelPOST(user, "pos_backend/pos_analysis", token, "SalesByClassification", { StartDate: InitialDate, EndDate: FinalDate });
}

async function ProfitabilityAnalysis(user, token, InitialDate, FinalDate) {
    return sendToAngelPOST(user, "pos_backend/pos_analysis", token, "ProfitabilityAnalysis", { StartDate: InitialDate, EndDate: FinalDate });
}


// Envia un mensaje a AngelSQLServer   
async function sendToAngelPOST(user, api_name, token, OperationType, object_data) {

  var main_ccount = "";

  if (user.includes("@")) {
    main_ccount = user.split("@")[1];
  }
  
  var api = {
    api: api_name,
    account: main_ccount,
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


async function sendPOST(data) {

  var url = "";

  if (typeof clientKey !== 'undefined' && clientKey) {
      url = window.location.protocol + '//' + window.location.host + "/" + clientKey + "/AngelPOST";
  }
  else
  {
      url = window.location.protocol + '//' + window.location.host + "/AngelPOST";
  }    

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

function generateButton(href, iconSrc, buttonText, buttonClass, onclick = {}) {
  let button = document.createElement("a");
  button.href = href;
  button.className = buttonClass;
  button.style.paddingRight = "120px";

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