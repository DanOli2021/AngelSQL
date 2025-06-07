async function login(user, password) {
    return sendToAngelPOST(user, "tokens/admintokens", "", "GetTokenFromUser", { User: user, Password: password });
}

async function GetGroupsUsingTocken(user, token) {
    return sendToAngelPOST(user, "tokens/admintokens", "", "GetGroupsUsingTocken", { TokenToObtainPermission: token });
}

async function GetUser(user, token, userToObtain) {
    return sendToAngelPOST(user, "tokens/admintokens", token, "GetUser", { User: userToObtain });
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
        url = window.location.protocol + '//' + window.location.host + "/" + clientKey + "/AngelPOST";
    }
    else {
        url = window.location.protocol + '//' + window.location.host + "/AngelPOST";
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


    if (typeof clientKey !== 'undefined' && clientKey) {
        url = window.location.protocol + '//' + window.location.host + "/" + clientKey + "/AngelPOST";
    }
    else {
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


function ShowDialog(title, message) {
    document.getElementById('generic_dialog_title').innerText = title;
    document.getElementById('generic_dialog_message').innerText = '⚠️' + message;
    document.getElementById('generic_dialog').showModal();
}

function CloseDialog() {
    document.getElementById('generic_dialog').close();
}

function ShowAcceptCancelDialog(title, message, callback) {
    document.getElementById('dialog_accept_title').innerText = title;
    document.getElementById('dialog_accept_message').innerText = '⚠️' + message;
    document.getElementById('dialog_button_accept').onclick = callback;
    document.getElementById('dialog_accept').showModal();
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

        let html = `<div class="table-responsive"><table class="table table-bordered table-striped"><thead><tr>`;

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