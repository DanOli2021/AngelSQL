async function login(user, password) {
    return sendToAngelPOST(user, "tokens/admintokens", "", "GetTokenFromUser", { User: user, Password: password });
}

async function GetGroupsUsingTocken(user, token) {
    return sendToAngelPOST(user, "tokens/admintokens", "", "GetGroupsUsingTocken", { TokenToObtainPermission: token });
}

async function GetUser(user, token, userToObtain) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "GetUser", { User: userToObtain });
}

async function SaveUser(user, token, userToSave) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "UpsertUser", userToSave);
}

async function GetUsers(user, token, Where = {}) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "GetUsers", Where);
}

async function DeleteToken(user, token, TokenToDelete) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "DeleteToken", { TokenToDelete: TokenToDelete });
}

async function GetTokens(user, token) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "GetTokens", {});
}

async function GetToken(user, token, TokenId) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "GetToken", { TokenId: TokenId });
}

async function SaveToken(user, token, Token) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "SaveToken", Token);
}

async function DeleteUser(user, token, UserToDelete) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "DeleteUser", { UserToDelete: UserToDelete });
}

async function GetGroups(user, token) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "GetGroups", {});
}

async function GetGroup(user, token, id) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "GetGroups", { Where: "id = '" + id + "'" });
}

async function SaveGroup(user, token, GroupToSave) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "UpsertGroup", GroupToSave);
}

async function DeleteGroup(user, token, GroupToDelete) {
  return sendToAngelPOST(user, "tokens/admintokens", token, "DeleteGroup", { UserGroupToDelete: GroupToDelete });
}

async function GetTopicsFromUser(user, token) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "GetTopicsFromUser", {});
}

async function GetTopics(user, token) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "GetTopics", {});
}

async function SaveTopic(user, token, topic) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "UpsertTopic", topic);
}

async function GetTopic(user, token, topic_id) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "GetTopic", { Id: topic_id });
}

async function GetSubTopicsFromTopic(user, token, topic_id) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "GetSubTopicsFromTopic", { Topic_id: topic_id });
}

async function GetSubTopic(user, token, subtopic_id) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "GetSubTopic", { Id: subtopic_id });
}

async function SaveSubTopic(user, token, subtopic) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "UpsertSubTopic", subtopic);
}

async function GetContentFromSubTopic(user, token, subtopic) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "GetContentFromSubTopic", { Subtopic_id: subtopic });
}

async function GetContent(user, token, Content_id) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "GetContent", { Id: Content_id });
}

async function SaveContent(user, token, content) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "UpsertContent", content);
}

async function DeleteContent(user, token, content_id) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "DeleteContent", { Content_id: content_id });
}

async function GetContentDetail(user, token, content_id) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "GetContentDetail", { Content_id: content_id });
}

async function GetContentDetailItem(user, token, id) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "GetContentDetailItem", { Id: id });
}

async function GetTitles(user, token, content_id) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "GetTitles", { Content_id: content_id });
}

async function SaveContentDetail(user, token, contentdetail) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "UpsertContentDetail", contentdetail);
}

async function DeleteContentDetail(user, token, id, Content_id) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "DeleteContentDetail", { Id: id, Content_id: Content_id } );
}

async function SendFileToDownload(user, token, file, dataMessage) {
  return SendFile(user, "docs/helpdesk", token, "UploadFile", file, dataMessage);
}

async function SearchInfo(user, token, textToSerch) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "SearchInfo", { Search: textToSerch });
}

async function DeleteSubTopic(user, token, id) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "DeleteSubTopic", { Id: id });
}

async function DeleteTopic(user, token, id) {
  return sendToAngelPOST(user, "docs/helpdesk", token, "DeleteTopic", { Id: id });
}

async function GetPublicContent(account, Content_id) {
    let user = "user@" + account;
    return sendToAngelPOST(user, "docs/helpdesk", "", "GetPublicContent", { Content_id: Content_id }, account);
}

async function GetContentTitles(account, Content_id) {
    let user = "user@" + account;
    return sendToAngelPOST(user, "docs/helpdesk", "", "GetContentTitles", { Content_id: Content_id }, account);
}

async function GetContentDetailCSS(account, Content_id) {
    let user = "user@" + account;
    return sendToAngelPOST(user, "docs/helpdesk", "", "GetContentDetailCSS", { Content_id: Content_id }, account);
}


function generateGUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = (Math.random() * 16) | 0;
        const v = c === 'x' ? r : (r & 0x3) | 0x8;
        return v.toString(16);
    });
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
            account: account,
            Token: token,
            User: user,
            DataMessage: object_data,
            UserLanguage: getSelectedLanguage()
        }
    };

    return await sendPOST(api);

}


async function sendAsyncToAngelPOST(user, api_name, token, OperationType, object_data, TaskGuid = "") {

    account = "";

    if (user.includes("@")) {
        account = user.split("@")[1];
    }

    var api = {
        api: api_name,
        account: account,
        language: "C#",
        TaskGuid: TaskGuid,
        GetResult: false,
        message:
        {
            OperationType: OperationType,
            account: account,
            Token: token,
            User: user,
            DataMessage: object_data,
            UserLanguage: getSelectedLanguage()
        }
    };

    return await sendPOSTAsync(api);

}


async function getResult(TaskGuid) {

    var api = {
        api: "",
        account: "",
        language: "",
        TaskGuid: TaskGuid,
        GetResult: true,
    };

    return await sendPOSTAsync(api);

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
            account: account,
            Token: token,
            User: user,
            DataMessage: dataMessage,
            UserLanguage: getSelectedLanguage()
        }
    };

    const formData = new FormData();
    formData.append('file', file);
    formData.append('jSonString', JSON.stringify(api));

    if (typeof clientKey !== 'undefined' && clientKey) {
        url = window.location.protocol + '//' + window.location.host + "/" + clientKey + "/AngelUpload";
    }
    else {
        url = window.location.protocol + '//' + window.location.host + "/AngelUpload";
    }

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


    if (typeof clientKey !== 'undefined' && clientKey != "none" && clientKey) {
        url = window.location.protocol + '//' + window.location.host + "/" + clientKey + "/AngelPOST";
    }
    else {
        url = window.location.protocol + '//' + window.location.host + "/AngelPOST";
    }

    console.log("Sending POST to: " + url);

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


async function sendPOSTAsync(data) {


    if (typeof clientKey !== 'undefined' && clientKey) {
        url = window.location.protocol + '//' + window.location.host + "/" + clientKey + "/AsyncPOST";
    }
    else {
        url = window.location.protocol + '//' + window.location.host + "/AsyncPOST";
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


function ThousandsSeparator(num) {

    num = parseFloat(num);

    if (isNaN(num)) {
        return "0.00";
    }

    return num.toLocaleString('es-MX', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    });

}


function generateButton(href, image_src, buttonText, buttonClass, onclick = {}, backgroundColor = "", color = "") {
    let button = document.createElement("a");
    button.href = href;
    button.className = buttonClass;
    button.style.paddingRight = "120px";

    if (backgroundColor != "") {
        button.style.backgroundColor = backgroundColor;
    }

    if (color != "") {
        button.style.color = color;
    }

    let iconSpan = document.createElement("span");
    iconSpan.className = "material-symbols-outlined";
    iconSpan.style.float = "left";

    let iconImg = document.createElement("img");
    iconImg.src = image_src;
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

function generateParagraph(element, text, classstring, stylestring) {
    let p = document.createElement(element);
    p.innerText = translate_element(getSelectedLanguage(), text);
    p.style.textAlign = stylestring;
    p.className = classstring;
    let div = document.getElementById("buttonszone");
    div.appendChild(p);
}


function ShowDialog(title, message, callback = null) {
    document.getElementById('generic_dialog_title').innerText = title;
    document.getElementById('generic_dialog_message').innerText = '⚠️' + message;
    document.getElementById('generic_dialog').showModal();

    if (callback) {
        callback();
    }

}

function showDialog(title, message) {
    // Elimina diálogos anteriores si existen
    const existingDialog = document.getElementById('genericDialog');
    if (existingDialog) existingDialog.remove();

    // Crear el HTML del diálogo
    const dialogHtml = document.createElement('dialog');
    dialogHtml.id = 'genericDialog';    
    dialogHtml.style.padding = '20px';
    dialogHtml.style.border = 'none';
    dialogHtml.style.borderRadius = '10px';
    dialogHtml.style.boxShadow = '0 0 20px rgba(0,0,0,0.4)';
    dialogHtml.innerHTML = `
        <form method="dialog">
            <h3 style="margin-top:0;">${title}</h3>
            <p>${message}</p>
            <div style="text-align:right;">
                <button id="dialogAcceptBtn" value="ok" class="btn btn-primary">Aceptar</button>
            </div>
        </form>
    `;

    // Inyectar al body
    document.body.appendChild(dialogHtml);

    // Mostrar el diálogo
    dialogHtml.showModal();

    // Cerrar al hacer clic en el botón o presionar Esc
    dialogHtml.querySelector('#dialogAcceptBtn').addEventListener('click', () => {
        dialogHtml.close();
        dialogHtml.remove();
    });

    // También quitarlo al cerrar manualmente
    dialogHtml.addEventListener('close', () => {
        dialogHtml.remove();
    });
}



function CloseDialog() {
    document.getElementById('generic_dialog').close();
}

function ShowAcceptCancelDialog(title, message, callback) {
    // Quitar diálogos anteriores si existen
    const existing = document.getElementById('dynamicAcceptDialog');
    if (existing) existing.remove();

    // Crear el diálogo
    const dialog = document.createElement('dialog');
    dialog.id = 'dynamicAcceptDialog';
    dialog.style.padding = '20px';
    dialog.style.border = 'none';
    dialog.style.borderRadius = '10px';
    dialog.style.boxShadow = '0 0 20px rgba(0,0,0,0.4)';
    dialog.innerHTML = `
        <form method="dialog">
            <h3>${title}</h3>
            <p style="margin-bottom: 20px;">⚠️ ${message}</p>
            <div style="text-align: right;">
                <button value="cancel" style="margin-right: 10px;" class="btn btn-secondary">Cancelar</button>
                <button id="confirmButton" value="accept" class="btn btn-primary">Aceptar</button>
            </div>
        </form>
    `;

    // Insertar y mostrar
    document.body.appendChild(dialog);
    dialog.showModal();

    // Manejar clic en aceptar
    dialog.querySelector('#confirmButton').addEventListener('click', () => {
        dialog.close();
        dialog.remove();
        callback(); // Ejecutar el callback
    });

    // Cerrar sin hacer nada si cancela
    dialog.addEventListener('close', () => {
        dialog.remove();
    });
}

function ShowAcceptCancelDialogClose() {
    document.getElementById('dialog_accept').close();
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


function compressAndPreviewImage(file, previewImg, inputId, fileContainerId) {
    const maxWidth = 300;
    const maxHeight = 300;

    const img = new Image();
    const reader = new FileReader();

    reader.onload = function (e) {
        img.src = e.target.result;
    };

    img.onload = function () {
        const canvas = document.createElement("canvas");
        let width = img.width;
        let height = img.height;

        // Ajustar dimensiones manteniendo proporción
        if (width > height) {
            if (width > maxWidth) {
                height *= maxWidth / width;
                width = maxWidth;
            }
        } else {
            if (height > maxHeight) {
                width *= maxHeight / height;
                height = maxHeight;
            }
        }

        canvas.width = width;
        canvas.height = height;

        const ctx = canvas.getContext("2d");
        ctx.drawImage(img, 0, 0, width, height);

        const compressedBase64 = canvas.toDataURL("image/jpeg", 0.6); // calidad 60%

        const mainImage = document.getElementById(previewImg);

        // Mostrar imagen forzando recarga
        mainImage.hidden = true;
        mainImage.src = ""; // limpia primero
        setTimeout(() => {
            document.getElementById(fileContainerId).value = "";
            mainImage.style.display = "block";
            mainImage.src = compressedBase64;
            mainImage.hidden = false;
        }, 10);

        // Limpiar el input file solo después de asignar imagen
        mainImage.onload = () => {
            document.getElementById(inputId).value = "";
        };
    };

    reader.readAsDataURL(file);
}


function openCenteredWindow(url) {
    const screenWidth = window.innerWidth;
    const screenHeight = window.innerHeight;

    const width = Math.floor(screenWidth / 2);
    const height = Math.floor(screenHeight / 2);
    const left = Math.floor((screenWidth - width) / 2);
    const top = Math.floor((screenHeight - height) / 2);

    window.open(
        url,
        '_blank',
        `width=${width},height=${height},left=${left},top=${top},resizable=yes,scrollbars=yes`
    );
}



function renderPaginatedTable(data, divId, rowsPerPage = 20, customColumns = {}, customSearchInput = null, exportToXlsx = true) {
    const container = document.getElementById(divId);
    let currentPage = 1;
    let filteredData = [...data];
    let currentSort = { column: null, asc: true };
    let searchText = "";

    const allHeaders = Object.keys(data[0]);
    const getVisibleHeaders = () => allHeaders.filter(h => customColumns[h]?.visible !== false);

    const totalPages = () => Math.ceil(filteredData.length / rowsPerPage);

    var searchInput = null;

    if (customSearchInput) {
        searchInput = document.getElementById(customSearchInput);
        if (searchInput) {
            searchInput.oninput = function () {
                searchText = this.value;
                filterTable(searchText);
            };
        }
    }

    var exportBtn;

    if (exportToXlsx == true) {
        exportBtn = document.createElement("button");
        exportBtn.className = "btn btn-success mb-3 ms-2";
        exportBtn.style = "margin-top: 10px;";
        exportBtn.innerText = "Export to Excel";
        exportBtn.onclick = exportToExcel;
    }

    const topControls = document.createElement("div");
    topControls.className = "d-flex justify-content-between align-items-center";

    if (customSearchInput) {
        searchInput.value = "";
    }

    if (exportToXlsx == true) {
        topControls.appendChild(exportBtn);
    }

    function sortData(column) {
        if (currentSort.column === column) {
            currentSort.asc = !currentSort.asc;
        } else {
            currentSort.column = column;
            currentSort.asc = true;
        }

        filteredData.sort((a, b) => {
            const valA = a[column];
            const valB = b[column];
            return currentSort.asc
                ? String(valA).localeCompare(String(valB))
                : String(valB).localeCompare(String(valA));
        });
    }

    function exportToExcel() {
        const ws = XLSX.utils.json_to_sheet(filteredData);
        const wb = XLSX.utils.book_new();
        XLSX.utils.book_append_sheet(wb, ws, "Datos");
        XLSX.writeFile(wb, "tabla_datos.xlsx");
    }

    function renderTable() {
        const visibleHeaders = getVisibleHeaders();
        const start = (currentPage - 1) * rowsPerPage;
        const end = start + rowsPerPage;
        const pageData = filteredData.slice(start, end);

        let html = `<div class="table-responsive" style="width:100% !important; max-width:100% !important;"><table class="table table-bordered table-striped" style="width:100%;"> <thead><tr>`;

        for (let header of visibleHeaders) {
            const colConf = customColumns[header] || {};
            const title = colConf.title || header;
            html += `<th style="cursor:pointer" onclick="sortTable('${header}')">${title} ` +
                (currentSort.column === header ? (currentSort.asc ? '▲' : '▼') : '') + `</th>`;
        }

        html += '</tr></thead><tbody>';

        for (let row of pageData) {
            // Detecta "id" sin importar el casing
            const rowId = row.id || row.ID || row.Id || "";
            html += `<tr${rowId ? ` id="${String(rowId).trim().toUpperCase()}"` : ''}>`;

            for (let i = 0; i < visibleHeaders.length; i++) {
                const header = visibleHeaders[i];
                const colValue = row[header];
                const colConf = customColumns[header] || {};
                const cellContent = colConf.html ? colConf.html(colValue, row) : colValue;
                let baseStyle = i === 0 ? 'white-space: normal; word-wrap: break-word;' : '';
                const cellStyle = ` style="${baseStyle}${colConf.style ? '; ' + colConf.style : ''}"`;
                html += `<td${cellStyle}>${cellContent}</td>`;
            }

            html += '</tr>';
        }

        html += '</tbody>';

        const hasTotals = visibleHeaders.some(h => customColumns[h]?.sum);
        if (hasTotals) {
            html += '<tfoot><tr>';
            for (let header of visibleHeaders) {
                const colConf = customColumns[header] || {};
                if (colConf.sum) {
                    const total = filteredData.reduce((acc, row) => acc + (parseFloat(row[header]) || 0), 0);
                    const formatted = colConf.sumFormatter ? colConf.sumFormatter(total) : total;
                    html += `<td${colConf.style ? ` style="${colConf.style}"` : ''}><strong>${formatted}</strong></td>`;
                } else {
                    html += '<td></td>';
                }
            }
            html += '</tr></tfoot>';
        }

        html += '</table></div>';

        html += `<nav><ul class="pagination justify-content-center">`;
        html += `<li class="page-item ${currentPage === 1 ? 'disabled' : ''}">` +
            `<button class="page-link" onclick="changePage(-1)">&#8592;</button></li>`;
        html += `<li class="page-item disabled"><span class="page-link">Page ${currentPage} of ${totalPages()}</span></li>`;
        html += `<li class="page-item ${currentPage === totalPages() ? 'disabled' : ''}">` +
            `<button class="page-link" onclick="changePage(1)">&#8594;</button></li>`;
        html += '</ul></nav>';

        container.style.width = "100%";
        container.style.maxWidth = "100%";
        container.style.overflowX = "auto";

        container.innerHTML = '';
        container.appendChild(topControls);
        container.insertAdjacentHTML('beforeend', html);

        setTimeout(() => {
            const rows = container.querySelectorAll("table tbody tr");

            rows.forEach((tr, rowIndex) => {
                const row = filteredData[(currentPage - 1) * rowsPerPage + rowIndex];
                const cells = tr.children;

                getVisibleHeaders().forEach((key, colIndex) => {
                    const conf = customColumns[key];
                    if (conf?.onclick) {
                        const td = cells[colIndex];
                        if (td) {
                            td.style.cursor = "pointer";
                            td.addEventListener("click", () => conf.onclick(row[key], row, row["Account_id"]));
                        }
                    }
                });
            });
        }, 0);
    }

    function filterTable(text) {
        currentPage = 1;
        filteredData = data.filter(row =>
            Object.values(row).some(val =>
                String(val).toLowerCase().includes(text.toLowerCase())
            )
        );
        renderTable();
    }

    window.changePage = function (offset) {
        currentPage += offset;
        if (currentPage < 1) currentPage = 1;
        if (currentPage > totalPages()) currentPage = totalPages();
        renderTable();
    };

    window.sortTable = function (column) {
        sortData(column);
        renderTable();
    };

    renderTable();
    container.insertBefore(topControls, container.firstChild);
}


function createDialog(message, onAccept) {
    // Crear el diálogo
    const dialog = document.createElement('dialog');
    dialog.style.padding = '20px';
    dialog.style.borderRadius = '8px';
    dialog.style.textAlign = 'center';
    dialog.style.minWidth = '250px';

    // Crear el mensaje del diálogo
    const dialogMessage = document.createElement('p');
    dialogMessage.textContent = message;
    dialog.appendChild(dialogMessage);

    // Crear el botón de aceptar
    const acceptButton = document.createElement('button');
    acceptButton.textContent = 'Aceptar';
    acceptButton.style.marginTop = '20px';
    acceptButton.style.padding = '10px 20px';
    acceptButton.style.border = 'none';
    acceptButton.style.borderRadius = '5px';
    acceptButton.style.backgroundColor = '#007bff';
    acceptButton.style.color = 'white';
    acceptButton.style.cursor = 'pointer';

    // Agregar el evento para el botón de aceptar
    acceptButton.addEventListener('click', () => {
        onAccept(); // Ejecutar la función proporcionada
        dialog.close(); // Cerrar el diálogo
        dialog.remove(); // Eliminar el diálogo del DOM
    });

    // Agregar el botón al diálogo
    dialog.appendChild(acceptButton);

    // Agregar el diálogo al body
    document.body.appendChild(dialog);

    // Mostrar el diálogo
    dialog.showModal();
}



function Round(num, precision = 2) {
    if (isNaN(num)) {
        return 0;
    }
    var factor = Math.pow(10, precision);
    return Math.round(num * factor) / factor;
}



function getDominantColor(imgElement, callback, borderSize = 10) {
    const canvas = document.createElement('canvas');
    const context = canvas.getContext('2d', { willReadFrequently: true });

    if (!imgElement.complete) {
        imgElement.onload = () => getDominantColor(imgElement, callback, borderSize);
        imgElement.onerror = () => console.error("No se pudo cargar la imagen.");
        return;
    }

    const width = imgElement.width;
    const height = imgElement.height;
    canvas.width = width;
    canvas.height = height;

    context.drawImage(imgElement, 0, 0, width, height);

    const colorCounts = {};
    let maxCount = 0;
    let dominantColor = { r: 255, g: 255, b: 255 };

    const processPixel = (x, y) => {
        const pixelData = context.getImageData(x, y, 1, 1).data;
        const r = pixelData[0], g = pixelData[1], b = pixelData[2], a = pixelData[3];

        if (a < 125 || (r > 240 && g > 240 && b > 240) || (r < 15 && g < 15 && b < 15)) {
            return;
        }

        const rgbString = `${r},${g},${b}`;
        colorCounts[rgbString] = (colorCounts[rgbString] || 0) + 1;

        if (colorCounts[rgbString] > maxCount) {
            maxCount = colorCounts[rgbString];
            dominantColor = { r, g, b };
        }
    };

    for (let x = 0; x < width; x++) {
        for (let y = 0; y < borderSize; y++) processPixel(x, y);
        for (let y = height - borderSize; y < height; y++) processPixel(x, y);
    }
    for (let y = borderSize; y < height - borderSize; y++) {
        for (let x = 0; x < borderSize; x++) processPixel(x, y);
        for (let x = width - borderSize; x < width; x++) processPixel(x, y);
    }

    const finalColor = `rgb(${dominantColor.r}, ${dominantColor.g}, ${dominantColor.b})`;
    callback(finalColor);
}


function LogOut() {
    localStorage.removeItem("Token");
    sessionStorage.removeItem("user_groups");
    sessionStorage.removeItem("Token");
    window.location.href = "index.html";
}


