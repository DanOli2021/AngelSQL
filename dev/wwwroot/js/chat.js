// --- Inicio: Simulación de Backend (Webhook) ---
// Este código simula un servidor Node.js muy básico con Express para manejar el webhook.
// En un entorno real, esto estaría en un archivo de servidor separado (ej. app.js o server.js).

// --- Variables Globales ---
let uploadedImageBase64 = null; // Variable global para almacenar la imagen en Base64

// Mapa de emojis para la conversión de texto a emoji
const emojiMap = {
    ':smile:': '😄',
    ':laugh:': '😂',
    ':heart:': '❤️',
    ':thumbsup:': '👍',
    ':clap:': '👏',
    ':thinking:': '🤔',
    ':wink:': '😉',
    ':grinning:': '😀',
    ':cry:': '😢',
    ':fire:': '🔥',
    ':star:': '⭐',
    ':rocket:': '🚀',
    ':sparkles:': '✨',
    ':tada:': '🎉',
    ':ok_hand:': '👌',
    ':pray:': '🙏',
    ':sunglasses:': '😎',
    ':metal:': '🤘',
    ':shrug:': '🤷‍♀️',
    ':facepalm:': '🤦‍♂️',
    ':check:': '✅',
    ':x:': '❌',
    ':question:': '❓',
    ':exclamation:': '❗',
    ':zap:': '⚡',
    ':pizza:': '🍕',
    ':beer:': '🍺',
    ':coffee:': '☕',
    ':dog:': '🐶',
    ':cat:': '🐱',
    ':money:': '💰',
    ':bulb:': '💡',
    ':gear:': '⚙️',
    ':robot:': '🤖',
};

let chat_id = null;
let source = null; // Variable para indicar de dónde proviene el mensaje (ej. "index.html")
let pollingInterval = null;
let main_user = null; // Variable para almacenar el usuario actual
let main_token = null; // Reemplaza con tu cuenta de AngelSQL
let mensajeEsperandoMostrado = false;
let Timestamp = null; // Variable para almacenar el timestamp del chat actual
let enviandoMensaje = false;

chat_id = localStorage.getItem("chat_id") || generateGUID();
localStorage.setItem("chat_id", chat_id);

// --- Funciones del Chat ---
function lanzarChatModal(source_page, user, token) {

    source = source_page; // Guarda la fuente del chat

    if (!chat_id || chat_id === "null" || chat_id === "") {
        chat_id = generateGUID();
        localStorage.setItem("chat_id", chat_id);
    }

    main_user = user; // Guarda el usuario actual
    main_token = token; // Guarda el token de autenticación

    // Si ya hay datos guardados, iniciar chat directo
    const email = localStorage.getItem("chat_email");
    const telefono = localStorage.getItem("chat_telefono");

    if (email && telefono) {
        mostrarChat(email, telefono);
        return;
    }

    // Si no hay datos, crear modal para pedirlos
    if (!document.getElementById("modalDatosChat")) {
        const modalHTML = `
        <div class="modal fade" id="modalDatosChat" tabindex="-1" aria-labelledby="chatLabel" aria-hidden="true">
          <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content rounded-4">
              <div class="modal-header bg-primary text-white rounded-top-4">
                <h5 class="modal-title" id="chatLabel">Iniciar chat</h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
              </div>
              <div class="modal-body">
                <form id="formDatosChat">
                  <div class="mb-3">
                    <label for="chat_email" class="form-label">Correo electrónico</label>
                    <input type="email" class="form-control" id="chat_email" required placeholder="cliente@ejemplo.com">
                  </div>
                  <div class="mb-3">
                    <label for="chat_telefono" class="form-label">Teléfono</label>
                    <input type="tel" class="form-control" id="chat_telefono" required placeholder="55 1234 5678">
                  </div>
                  <button type="submit" class="btn btn-success w-100">Continuar al chat</button>
                </form>
              </div>
            </div>
          </div>
        </div>
      `;

        const div = document.createElement("div");
        div.innerHTML = modalHTML;
        document.body.appendChild(div.firstElementChild);

        // Agrega evento de envío
        document.getElementById("formDatosChat").addEventListener("submit", function (e) {
            e.preventDefault();
            const email = document.getElementById("chat_email").value.trim();
            const telefono = document.getElementById("chat_telefono").value.trim();

            // Guardar en localStorage
            localStorage.setItem("chat_email", email);
            localStorage.setItem("chat_telefono", telefono);

            // Cerrar modal e iniciar chat
            const modal = bootstrap.Modal.getInstance(document.getElementById("modalDatosChat"));
            modal.hide();
            mostrarChat(email, telefono);
        });
    }

    const modal = new bootstrap.Modal(document.getElementById("modalDatosChat"));
    modal.show();


}

function handleImageUpload(event) {
    const file = event.target.files[0];
    if (file) {
        convertirImagenABase64JPG(file, (jpgBase64) => {
            uploadedImageBase64 = jpgBase64;
            const imagePreview = document.getElementById('imagePreview');
            imagePreview.src = uploadedImageBase64;
            document.getElementById('imagePreviewContainer').classList.remove('d-none');
        });
        reader.readAsDataURL(file);
    }
}

function removeImagePreview() {
    uploadedImageBase64 = null;
    document.getElementById('chatImageUpload').value = ''; // Limpiar el input de archivo
    document.getElementById('imagePreviewContainer').classList.add('d-none');
    document.getElementById('imagePreview').src = '#';
}

function handlePaste(event) {
    const items = (event.clipboardData || event.originalEvent.clipboardData).items;
    for (let i = 0; i < items.length; i++) {
        if (items[i].type.indexOf("image") !== -1) {
            const file = items[i].getAsFile();
            const reader = new FileReader();
            convertirImagenABase64JPG(file, (jpgBase64) => {
                uploadedImageBase64 = jpgBase64;
                const imagePreview = document.getElementById('imagePreview');
                imagePreview.src = uploadedImageBase64;
                document.getElementById('imagePreviewContainer').classList.remove('d-none');
            });
            reader.readAsDataURL(file);
            event.preventDefault(); // Evita que el contenido de la imagen se pegue como texto
            break;
        }
    }
}

function mostrarChat(email, telefono) {
    if (!document.getElementById("modalChatCliente")) {
        const modalChatHTML = `
        <div class="modal fade" id="modalChatCliente" tabindex="-1" aria-labelledby="modalChatLabel" aria-hidden="true">
          <div class="modal-dialog modal-dialog-centered modal-lg">
            <div class="modal-content rounded-4">
              <div class="modal-header bg-dark text-white">
                <h5 class="modal-title" id="modalChatLabel">Chat con soporte</h5>
                <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
              </div>
              <div class="modal-body">
                <p><strong>Correo:</strong> ${email}<br><strong>Teléfono:</strong> ${telefono}</p>
                <div id="chatContenido" class="border rounded p-3 mb-3" style="height: 300px; overflow-y: auto; display: flex; flex-direction: column;">
                  <div style="max-width: 75%; word-wrap: break-word; background-color: #f0f0f0; color: #000; align-self: flex-start; border-radius: 0.75rem; padding: 0.5rem; box-shadow: 0 1px 2px rgba(0,0,0,0.1); margin-top: 0.5rem; margin-bottom: 0.5rem;">
                    <strong>Soporte:</strong> ¡Hola! ¿Cómo te podemos ayudar hoy?
                  </div>
                </div>


                <div id="mensajeLocating" class="text-muted text-center my-2">Locating... Waiting for the operator...</div>

                <div class="input-group">
                  <input type="text" class="form-control" id="chatInput" placeholder="Escribe tu mensaje...">
                  <button class="btn btn-outline-secondary" id="emojiButton" type="button" title="Seleccionar emoji">😊</button>
                  <label class="btn btn-outline-secondary" for="chatImageUpload" title="Adjuntar imagen">
                      📸<input type="file" id="chatImageUpload" accept="image/*" style="display:none;">
                  </label>
                  <button class="btn btn-outline-primary" onclick="enviarMensajeChat()">Enviar</button>
                </div>
                <div id="imagePreviewContainer" class="mt-2 d-none">
                    <img id="imagePreview" src="#" alt="Previsualización de imagen" class="img-fluid rounded" style="max-height: 150px;">
                    <button class="btn btn-sm btn-danger ms-2" onclick="removeImagePreview()">Remover</button>
                </div>
                <div id="emojiPicker" class="position-absolute bg-white border rounded p-2 shadow-sm d-none" style="z-index: 1000; bottom: 60px; left: 15px;">
                </div>
              </div>
            </div>
          </div>
        </div>
        <div class="modal fade" id="modalZoomImagen" tabindex="-1">
            <div class="modal-dialog modal-dialog-centered">
                <div class="modal-content bg-dark p-2">
                <img id="zoomImagenChat" src="#" class="img-fluid rounded" style="max-height: 80vh;">
                </div>
            </div>
        </div>
      `;
        const div = document.createElement("div");
        div.innerHTML = modalChatHTML;
        document.body.appendChild(div.firstElementChild);

        document.getElementById("chatInput").focus();

        // Rellenar el emojiPicker con los emojis del emojiMap
        const emojiPicker = document.getElementById("emojiPicker");
        for (const shortcut in emojiMap) {
            const span = document.createElement("span");
            span.textContent = emojiMap[shortcut];
            span.style.cursor = 'pointer';
            span.style.margin = '3px';
            emojiPicker.appendChild(span);
        }
    }

    const chatModal = new bootstrap.Modal(document.getElementById("modalChatCliente"));
    chatModal.show();
    iniciarVerificacionRespuestas();

    document.getElementById("modalChatCliente").addEventListener("hidden.bs.modal", () => {
        clearInterval(pollingInterval);
        pollingInterval = null;
    });

    if (chat_id === null || chat_id === undefined || chat_id === "null") {
        let response = sendToAngelPOST(Token.User, "chat/pos_chat", Token.Token, "GetChat", { "Chat_id": chat_id });

        response.then((query) => {

            if (query.startsWith("Error:")) {
                ShowDialog("Alert", query);
                return;
            }

            let query_result = JSON.parse(query);

            if (query_result.result.startsWith("Error:")) {
                ShowDialog("Alert", query_result.result);
                return;
            }

            const chatData = JSON.parse(query_result.result);

            chatData.forEach(element => {
                mostrarRespuestaSoporte(element.Message_text);
            });

        }).catch((err) => {

            ShowDialog("Alert", err);
        });

    }

    // Agrega ENTER para enviar, carga de imagen y selector de emojis
    setTimeout(() => {
        const chatInput = document.getElementById("chatInput");
        const emojiButton = document.getElementById("emojiButton");
        const emojiPicker = document.getElementById("emojiPicker");

        chatInput.addEventListener("keydown", function (e) {
            if (e.key === "Enter") {
                e.preventDefault();
                enviarMensajeChat();
            }
        });

        document.getElementById("chatImageUpload").addEventListener("change", handleImageUpload);
        chatInput.addEventListener("paste", handlePaste);

        emojiButton.addEventListener("click", (e) => {
            e.stopPropagation(); // Evita que el clic se propague al documento y cierre el picker inmediatamente
            emojiPicker.classList.toggle("d-none");
        });

        emojiPicker.addEventListener("click", (e) => {
            if (e.target.tagName === 'SPAN') {
                chatInput.value += e.target.textContent;
                emojiPicker.classList.add("d-none");
                chatInput.focus();
            }
        });

        // Ocultar el selector de emojis si se hace clic fuera del picker o el botón
        document.addEventListener("click", function (event) {
            if (emojiPicker && emojiButton && !emojiPicker.contains(event.target) && !emojiButton.contains(event.target)) {
                emojiPicker.classList.add("d-none");
            }
        });

    }, 300); // pequeño delay para asegurar que el input ya está en el DOM
}


function iniciarVerificacionRespuestas() {
    if (pollingInterval) return; // evita múltiples timers

    pollingInterval = setInterval(async () => {
        try {

            console.log("Polling for new replies...");  

            GetMessages();

        } catch (err) {
            console.error("Error An error occurred while checking for new replies:", err);
        }
    }, 5000); // cada 5 segundos, puedes ajustar este valor
}


async function GetMessages() {
    const email = localStorage.getItem("chat_email");
    const telefono = localStorage.getItem("chat_telefono");

    const chat = {
        Chat_id: chat_id, // Se usa para buscar respuestas a este chat
        Email: email,
        Phone: telefono,
        Source: source,
        Timestamp: Timestamp // Envía el timestamp del último mensaje recibido
    };

    let user;

    if (user === "Anonymous" || user === null || user === undefined) {
        user = "user@" + angelsql_account;
    }
    else {
        user = main_user; // Usa el usuario actual                
    }

    const response = await sendToAngelPOST(user, "Chat/pos_chat", main_token, "GetContactChatReply", chat);

    if (response && !response.startsWith("Error:")) {
        const json = JSON.parse(response);

        if (json.result.startsWith("Error:")) {
            return;
        }

        const respuestas = JSON.parse(json.result);

        respuestas.forEach(respuesta => {

            if (respuesta && respuesta.Message_text) {
                mostrarRespuestaSoporte(respuesta);
                Timestamp = respuesta.Timestamp; // Actualiza el timestamp al del último mensaje recibido
            }
        });

    }

}


function mostrarRespuestaSoporte(respuesta) {
    if (!mensajeEsperandoMostrado) {
        const locatingMsg = document.querySelector("#mensajeLocating");
        if (locatingMsg) locatingMsg.remove();
        mensajeEsperandoMostrado = true;
    }

    const contenedor = document.getElementById("chatContenido");

    const esCliente = !respuesta.Kiosko_user_id;

    const respuestaContenedor = document.createElement("div");
    respuestaContenedor.style.display = 'flex';
    respuestaContenedor.style.justifyContent = esCliente ? 'flex-end' : 'flex-start';
    respuestaContenedor.style.marginTop = '0.5rem';
    respuestaContenedor.style.marginBottom = '0.5rem';

    const respuestaBurbuja = document.createElement("div");
    respuestaBurbuja.style.maxWidth = '75%';
    respuestaBurbuja.style.wordWrap = 'break-word';
    respuestaBurbuja.style.borderRadius = '0.75rem';
    respuestaBurbuja.style.padding = '0.5rem';
    respuestaBurbuja.style.boxShadow = '0 1px 2px rgba(0,0,0,0.1)';
    respuestaBurbuja.style.backgroundColor = esCliente ? '#dcf8c6' : '#f0f0f0'; // verde claro o gris
    respuestaBurbuja.style.color = '#000';

    let texto = respuesta.Message_text || "No hay mensaje disponible";
    let imagen = respuesta.Image_data;

    if (!esCliente) {
        respuestaBurbuja.innerHTML = `<strong>${respuesta.Kiosko_user_id}:</strong> ${replaceEmojis(texto)}`;
    } else {
        respuestaBurbuja.innerHTML = `${replaceEmojis(texto)}`;
    }

    if (imagen) {
        respuestaBurbuja.innerHTML += `<br><img src="${imagen}" class="img-fluid rounded mt-2 shadow-sm" style="max-width: 200px; max-height: 200px; cursor: zoom-in;">`;
    }

    const horaSoporte = document.createElement("div");
    horaSoporte.style.fontSize = "0.75rem";
    horaSoporte.style.color = "#888";
    horaSoporte.style.marginTop = "0.25rem";
    horaSoporte.style.textAlign = esCliente ? "right" : "left";
    horaSoporte.textContent = new Date().toLocaleString();

    respuestaContenedor.appendChild(respuestaBurbuja);
    respuestaContenedor.appendChild(horaSoporte);
    contenedor.appendChild(respuestaContenedor);

    respuestaContenedor.style.animation = "parpadeo 1s ease-in-out 3";

    const style = document.createElement("style");
    style.innerHTML = `
        @keyframes parpadeo {
            0%, 100% { opacity: 1; }
            50% { opacity: 0; }
        }`;
    document.head.appendChild(style);

    contenedor.scrollTop = contenedor.scrollHeight;
}


/**
* Envía el mensaje actual del chat, procesando URLs, imágenes y emojis,
* y simula el envío a un backend.
*/
async function enviarMensajeChat() {
    if (enviandoMensaje) return; // Previene múltiples envíos
    enviandoMensaje = true;

    const input = document.getElementById("chatInput");
    input.disabled = true;

    if (pollingInterval) clearInterval(pollingInterval);
    pollingInterval = null;

    const email = localStorage.getItem("chat_email");
    const telefono = localStorage.getItem("chat_telefono");

    if (!chat_id || chat_id === "null" || chat_id === "") {
        chat_id = generateGUID();
        localStorage.setItem("chat_id", chat_id);
    }

    let mensajeTexto = input.value.trim();
    const contenedor = document.getElementById("chatContenido");

    const chat = {
        Chat_id: chat_id,
        Email: email,
        Phone: telefono,
        Message_text: mensajeTexto,
        Image_data: uploadedImageBase64,
        Source: source
    };

    try {
        let user = main_user || ("user@" + angelsql_account);
        const response = await sendToAngelPOST(user, "Chat/pos_chat", main_token, "InsertContactChat", chat);

        if (!response || response.startsWith("Error:")) {
            showDialog("Alert", response || "No se pudo conectar al servidor.");
            return;
        }

        const result = JSON.parse(response);
        if (result.result.startsWith("Error:")) {
            showDialog("Alert", result.result);
            return;
        }

    } catch (error) {
        console.error("Error de red al enviar mensaje:", error);
    }

    input.value = "";
    removeImagePreview();
    contenedor.scrollTop = contenedor.scrollHeight;
    input.focus();

    await GetMessages();
    iniciarVerificacionRespuestas();

    input.disabled = false;
    enviandoMensaje = false;
}


/**
 * Reemplaza las cadenas de texto (atajos) por sus emojis correspondientes.
 * Esta función busca y sustituye los atajos definidos en 'emojiMap'
 * por los caracteres emoji Unicode equivalentes.
 *
 * @param {string} text - El texto de entrada que puede contener atajos de emojis.
 * @returns {string} El texto con los atajos de emojis reemplazados por los emojis reales.
 */
function replaceEmojis(text) {
    let result = text;
    // Itera sobre cada atajo de emoji en el mapa
    for (const shortcut in emojiMap) {
        // Crea una expresión regular global para encontrar todas las ocurrencias del atajo.
        // shortcut.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&') escapa caracteres especiales
        // en el atajo para que la RegExp no los interprete como parte de su sintaxis.
        const regex = new RegExp(shortcut.replace(/[-\/\\^$*+?.()|[\]{}]/g, '\\$&'), 'g');
        // Reemplaza todas las ocurrencias del atajo con su emoji correspondiente
        result = result.replace(regex, emojiMap[shortcut]);
    }
    return result;
}


function convertirImagenABase64JPG(file, callback) {
    const reader = new FileReader();
    reader.onload = function (e) {
        const img = new Image();
        img.onload = function () {
            const canvas = document.createElement("canvas");
            canvas.width = img.width;
            canvas.height = img.height;
            const ctx = canvas.getContext("2d");
            ctx.drawImage(img, 0, 0);
            const jpgBase64 = canvas.toDataURL("image/jpeg", 0.8); // calidad entre 0 y 1
            callback(jpgBase64);
        };
        img.src = e.target.result;
    };
    reader.readAsDataURL(file);
}


document.addEventListener("click", function (e) {
    if (e.target.tagName === "IMG" && e.target.closest("#chatContenido")) {
        const zoomImg = document.getElementById("zoomImagenChat");
        zoomImg.src = e.target.src;
        const modalZoom = new bootstrap.Modal(document.getElementById("modalZoomImagen"));
        modalZoom.show();
    }
});
