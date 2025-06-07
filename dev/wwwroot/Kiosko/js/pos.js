var textSku;
var btnItems_text;
var btnTotal_text;
var sale_detail;
var search_result;
var searchcustomer_result;
var sale = undefined;
var sale_detail_style = 0;
var language;
var Token;
var localCustomer;
var customer_text;
var dialog_customer;
var customer_dialog_button_close;
var customer_dialog_button_accept;
var btnSearchCustomer;
var btnConfirmSale;
var btnNewCustomer;
var dialog_sku_edit;
var dialog_skuedit_close;
var item_to_edit;
var businessinfo = undefined;
var currencies = undefined;
var payments = undefined;
var localpayments = undefined;
var Currency_id = "USD";
var default_currency = undefined;

function SaveCustomer() {

    let customer = new Customer();
    customer.Customer_id = document.getElementById("customer_id").value;
    customer.Customer_name = document.getElementById("customer_name").value;
    customer.Phone = document.getElementById("customer_phone").value;
    customer.Email = document.getElementById("customer_email").value;
    customer.Address = document.getElementById("customer_address").value;
    customer.City = document.getElementById("customer_city").value;
    customer.State = document.getElementById("customer_state").value;
    customer.Country = document.getElementById("customer_country").value;
    customer.CP = document.getElementById("customer_zip").value;
    customer.RFC = document.getElementById("customer_rfc").value;
    customer.BusinessLine_id = document.getElementById("business_line").value;
    customer.BusinessLine_description = document.getElementById("business_line_description").value;
    customer.DateTime = formatDate(new Date());

    var response = sendToAngelPOST(Token.User, "pos_backend/pos_backend", Token.Token, "SaveCustomer", customer);

    response.then(function (query) {

        if (query.startsWith("Error:")) {
            ShowDialog("Alert", query);
            return;
        }

        let responce_query = JSON.parse(query);

        if (responce_query.result.startsWith("Error:")) {
            ShowDialog("Alert", responce_query.result);
            return;
        }

        ShowDialog("Ok.", "Customer saved successfully");

        let customer_id = responce_query.result;
        SetCustomer(customer_id);

        localCustomer = customer;

    });

}

function GetBusinessLines() {
    var response = sendToAngelPOST(Token.User, "pos_backend/pos_backend", Token.Token, "GetBusinessLines", "");

    response.then(function (query) {

        if (query.startsWith("Error:")) {
            ShowDialog("Alert", query);
            return;
        }

        let responce_query = JSON.parse(query);

        if (responce_query.result.startsWith("Error:")) {
            ShowDialog("Alert", responce_query.result);
            return;
        }

        if (responce_query.result == "[]") {
            ShowDialog("Alert", "No data found for Business Lines");
            return;
        }

        let business_lines = JSON.parse(responce_query.result);

        var business_line = document.getElementById("business_line");
        business_line.innerHTML = "";

        for (let i = 0; i < business_lines.length; i++) {
            let option = document.createElement("option");
            option.text = business_lines[i].id + " " + business_lines[i].Description;
            option.value = business_lines[i].id;
            business_line.add(option);
        }


    });

}


async function GetCurrencies() {

    var response = await sendToAngelPOST(Token.User, "pos_backend/pos_backend", Token.Token, "GetCurrencies", "");

    if (response.startsWith("Error:")) {
        ShowDialog("Alert", response);
        return;
    }

    if (response.startsWith("Error:")) {
        ShowDialog("Alert", response);
        return; // Devolver el objeto predeterminado en caso de error
    }

    let responce_query = JSON.parse(response);

    if (responce_query.result.startsWith("Error:")) {
        ShowDialog("Alert", responce_query.result);
        return; // Devolver el objeto predeterminado en caso de error
    }

    currencies = JSON.parse(responce_query.result);

    for (var i = 0; i < currencies.length; i++) {
        //Agregando cada moneda al select currency_id
        var cur = document.createElement("option");
        cur.text = currencies[i].id + "," + currencies[i].Symbol + "," + currencies[i].Description;
        cur.value = currencies[i].id;
        document.getElementById("currency_id").add(cur);
    }

    document.getElementById("currency_id").onchange = function () {

        var selectedCurrency = document.getElementById("currency_id").value;

        for (var i = 0; i < currencies.length; i++) {
            if (currencies[i].id == selectedCurrency) {
                document.getElementById("exchange_rate").value = currencies[i].Exchange_rate;
                break;
            }
        }
    }

    document.getElementById("currency_id").value = default_currency.Id;
    document.getElementById("exchange_rate").value = 1;

}


async function GetDefaultCurrency() {
    const query = await sendToAngelPOST(Token.User, "pos_backend/pos_parameters", Token.Token, "GetDefaultCurrency", "Currency");

    if (query.startsWith("Error:")) {
        ShowDialog("Alert", query);
        return "USD"; // Devolver el valor predeterminado en caso de error
    }

    const responce_query = JSON.parse(query);

    if (responce_query.result.startsWith("Error:")) {
        ShowDialog("Alert", responce_query.result);
        return "USD"; // Devolver el valor predeterminado en caso de error
    }

    if (responce_query.result == "[]") {
        return "USD"; // Devolver el valor predeterminado si no hay datos
    }

    default_currency = JSON.parse( responce_query.result );
    return default_currency; // Devolver el valor de la moneda predeterminada

}


async function GetBusinessInfo() {

    // Crear un objeto BusinessInfo predeterminado
    let bi = new BusinessInfo(
        "1",
        "",
        "Kiosko",
        "",
        "info@angelsql.net",
        "https://angelsql.net",
        "images/Kiosko_logo.png",
        "The best helper for your business",
        "USD",
        "US Dollar"
    );

    try {
        // Llamar al backend
        const query = await sendToAngelPOST(Token.User, "pos_backend/pos_businessinfo", Token.Token, "GetBusinessInfo", "");

        if (query.startsWith("Error:")) {
            ShowDialog("Alert", query);
            return bi; // Devolver el objeto predeterminado en caso de error
        }

        const responce_query = JSON.parse(query);

        if (responce_query.result.startsWith("Error:")) {
            ShowDialog("Alert", responce_query.result);
            return bi; // Devolver el objeto predeterminado en caso de error
        }

        if (responce_query.result == "[]") {
            return bi; // Devolver el objeto predeterminado si no hay datos
        }

        // Actualizar el objeto bi con los datos recibidos
        bi = JSON.parse(responce_query.result);
        return bi; // Devolver el objeto actualizado

    } catch (error) {

        console.error("Error fetching business info:", error);
        ShowDialog("Alert", "An error occurred while fetching business info.");
        return bi; // Devolver el objeto predeterminado en caso de excepción

    }
}

function SearchCustomers() {

    var response = sendToAngelPOST(Token.User, "pos_backend/pos_backend", Token.Token, "GetCustomers", textSku.value);

    response.then(function (query) {

        if (query.startsWith("Error:")) {
            ShowDialog("Alert", query);
            return;
        }

        let responce_query = JSON.parse(query);

        if (responce_query.result.startsWith("Error:")) {
            ShowDialog("Alert", responce_query.result);
            return;
        }

        if (responce_query.result == "[]") {
            ShowDialog("Alert", "No data found for Description: " + textSku.value);
            textSku.value = "";
            return;
        }

        let customers = JSON.parse(responce_query.result);

        search_result.innerHTML = "";

        // Creamos un arreglo para almacenar los botones
        let buttons = [];
        var int_style = 0;

        for (let i = 0; i < customers.length; i++) {

            let customer = customers[i];
            let button = document.createElement("button");
            button.innerHTML = customer.id + " " + customer.Name + "<br>" + customer.Phone;

            if (int_style == 0) {
                button.className = "btn btn-primary w-100 h-100 d-flex flex-column align-items-center justify-content-center";
                int_style = 1;
            }
            else {
                button.className = "btn btn-success w-100 h-100 d-flex flex-column align-items-center justify-content-center";
                int_style = 0;
            }

            button.style = "margin-bottom: 5px";
            button.id = "Customer_" + customer.id;
            button.tabIndex = i + 1; // Establecemos el tabIndex para poder enfocar los botones

            button.addEventListener("click", function () {
                SetCustomer(customer.id);
                // Eliminamos el listener de teclado al seleccionar un Cliente
                document.removeEventListener('keydown', handleKeyDown);
                search_result.close();
            });

            buttons.push(button); // Añadimos el botón al arreglo
            search_result.appendChild(button);
        }

        let close_button = document.createElement("button");
        close_button.innerHTML = "Close";
        close_button.className = "btn btn-warning w-100 h-200 d-flex flex-column align-items-center justify-content-center";
        close_button.style = "margin-bottom: 5px; margin-top: 10px; height: 50px";
        close_button.id = "Customer_close_button";
        close_button.tabIndex = customers.length + 1; // Aseguramos que el botón de cerrar tenga un tabIndex

        close_button.addEventListener("click", function () {
            search_result.close();
            // Eliminamos el listener de teclado al cerrar el diálogo
            document.removeEventListener('keydown', handleKeyDown);
        });

        textSku.value = "";

        search_result.appendChild(close_button);
        search_result.showModal();

        // Agregamos la funcionalidad de navegación con teclado
        if (buttons.length > 0) {
            let currentIndex = 0;
            buttons[currentIndex].focus();

            function handleKeyDown(event) {
                if (event.key === 'ArrowDown') {
                    event.preventDefault();
                    currentIndex = (currentIndex + 1) % buttons.length;
                    buttons[currentIndex].focus();
                } else if (event.key === 'ArrowUp') {
                    event.preventDefault();
                    currentIndex = (currentIndex - 1 + buttons.length) % buttons.length;
                    buttons[currentIndex].focus();
                } else if (event.key === 'Enter') {
                    event.preventDefault();
                    buttons[currentIndex].click();
                } else if (event.key === 'Escape') {
                    event.preventDefault();
                    search_result.close();
                    document.removeEventListener('keydown', handleKeyDown);
                }
            }

            // Agregamos el listener de teclado
            document.addEventListener('keydown', handleKeyDown);

            // Eliminamos el listener cuando se cierra el diálogo
            search_result.addEventListener('close', function () {
                document.removeEventListener('keydown', handleKeyDown);
            });
        }

    });

}


function ShowCustomerDialog() {
    if (localCustomer == undefined) {
        ShowDialog("Alert", "First you have to select the client");
        return;
    }

    var customer_id = document.getElementById("customer_id");
    customer_id.value = localCustomer.Customer_id;
    customer_id.disabled = true;

    var customer_name = document.getElementById("customer_name");
    customer_name.value = localCustomer.Customer_name;
    customer_name.focus();

    document.getElementById("customer_phone").value = localCustomer.Phone;
    document.getElementById("customer_email").value = localCustomer.Email;
    document.getElementById("customer_phone").value = localCustomer.Phone;
    document.getElementById("customer_address").value = localCustomer.Address;
    document.getElementById("customer_city").value = localCustomer.City;
    document.getElementById("customer_state").value = localCustomer.State;
    document.getElementById("customer_country").value = localCustomer.Country;
    document.getElementById("customer_zip").value = localCustomer.CP;
    document.getElementById("customer_rfc").value = localCustomer.RFC;
    document.getElementById("business_line").value = localCustomer.BusinessLine_id;
    document.getElementById("business_line_description").value = localCustomer.BusinessLine_description;

    document.getElementById("dialog_customer").showModal();

}


function ShowConfirmDialog() {

    //document.getElementById("total_confirm").innerHTML = "Total: $" + ThousandsSeparator(sale.Total);
    CalculatePayment();
    var dialog = document.getElementById("dialog_sale_confirm");
    dialog.showModal();
}



function NewCustomer() {
    var customer_id = document.getElementById("customer_id");
    customer_id.value = "New";
    customer_id.disabled = true;
    var customer_name = document.getElementById("customer_name");
    customer_name.value = "";
    customer_name.focus();
    document.getElementById("customer_phone").value = "";
    document.getElementById("customer_email").value = "";
    document.getElementById("customer_phone").value = "";
    document.getElementById("customer_address").value = "";
    document.getElementById("customer_city").value = "";
    document.getElementById("customer_state").value = "";
    document.getElementById("customer_country").value = "";
    document.getElementById("customer_zip").value = "";
    document.getElementById("customer_rfc").value = "";
    document.getElementById("business_line").value = "";
    document.getElementById("business_line_description").value = "";
    document.getElementById("dialog_customer").showModal();
}

function SearchSkus() {

    var response = sendToAngelPOST(Token.User, "pos_backend/pos_skus", Token.Token, "GetSkus", textSku.value);

    response.then(function (query) {

        if (query.startsWith("Error:")) {
            ShowDialog("Alert", query);
            return;
        }

        let responce_query = JSON.parse(query);

        if (responce_query.result.startsWith("Error:")) {
            ShowDialog("Alert", responce_query.result);
            return;
        }

        if (responce_query.result == "[]") {
            ShowDialog("Alert", "No data found for Description: " + textSku.value);
            textSku.value = "";
            return;
        }

        let skus = JSON.parse(responce_query.result);

        search_result.innerHTML = "";

        // Creamos un arreglo para almacenar los botones
        let buttons = [];
        var int_style = 0;

        for (let i = 0; i < skus.length; i++) {

            let sku = skus[i];
            //let impuestos = JSON.parse(skus[i].Consumption_taxes);
            let price = sku.Price;

            // for (let j = 0; j < impuestos.length; j++) {
            //     let tax = sku.Price * (impuestos[j].Rate / 100);
            //     price += tax;
            // }

            let button = document.createElement("button");
            button.innerHTML = sku.id + " " + sku.Description + "<br>" + "$" + price.toFixed(2);

            if (int_style == 0) {
                button.className = "btn btn-primary w-100 h-100 d-flex flex-column align-items-center justify-content-center";
                int_style = 1;
            }
            else {
                button.className = "btn btn-success w-100 h-100 d-flex flex-column align-items-center justify-content-center";
                int_style = 0;
            }

            button.style = "margin-bottom: 5px";
            button.id = "Sku_" + sku.Sku;
            button.tabIndex = i + 1; // Establecemos el tabIndex para poder enfocar los botones

            button.addEventListener("click", function () {
                AddItem(sku.id);
                // Eliminamos el listener de teclado al seleccionar un SKU
                document.removeEventListener('keydown', handleKeyDown);
                search_result.close();
            });

            buttons.push(button); // Añadimos el botón al arreglo
            search_result.appendChild(button);
        }

        let close_button = document.createElement("button");
        close_button.innerHTML = "Close";
        close_button.className = "btn btn-warning w-100 h-200 d-flex flex-column align-items-center justify-content-center";
        close_button.style = "margin-bottom: 5px; margin-top: 10px; height: 50px";
        close_button.id = "Sku_close_button";
        close_button.tabIndex = skus.length + 1; // Aseguramos que el botón de cerrar tenga un tabIndex

        close_button.addEventListener("click", function () {
            search_result.close();
            // Eliminamos el listener de teclado al cerrar el diálogo
            document.removeEventListener('keydown', handleKeyDown);
        });

        textSku.value = "";

        search_result.appendChild(close_button);
        search_result.showModal();

        // Agregamos la funcionalidad de navegación con teclado
        if (buttons.length > 0) {
            let currentIndex = 0;
            buttons[currentIndex].focus();

            function handleKeyDown(event) {
                if (event.key === 'ArrowDown') {
                    event.preventDefault();
                    currentIndex = (currentIndex + 1) % buttons.length;
                    buttons[currentIndex].focus();
                } else if (event.key === 'ArrowUp') {
                    event.preventDefault();
                    currentIndex = (currentIndex - 1 + buttons.length) % buttons.length;
                    buttons[currentIndex].focus();
                } else if (event.key === 'Enter') {
                    event.preventDefault();
                    buttons[currentIndex].click();
                } else if (event.key === 'Escape') {
                    event.preventDefault();
                    search_result.close();
                    document.removeEventListener('keydown', handleKeyDown);
                }
            }

            // Agregamos el listener de teclado
            document.addEventListener('keydown', handleKeyDown);

            // Eliminamos el listener cuando se cierra el diálogo
            search_result.addEventListener('close', function () {
                document.removeEventListener('keydown', handleKeyDown);
            });
        }

    });
}



function generateGUID() {
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, function (c) {
        const r = (Math.random() * 16) | 0;
        const v = c === 'x' ? r : (r & 0x3) | 0x8;
        return v.toString(16);
    });
}

function formatDate(fecha) {
    const year = fecha.getFullYear();
    const month = ('0' + (fecha.getMonth() + 1)).slice(-2);
    const day = ('0' + fecha.getDate()).slice(-2);
    const hours = ('0' + fecha.getHours()).slice(-2);
    const minutes = ('0' + fecha.getMinutes()).slice(-2);
    const seconds = ('0' + fecha.getSeconds()).slice(-2);

    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
}


function SetCustomer(Customer_id) {
    var response = sendToAngelPOST(Token.User, "pos_backend/pos_backend", Token.Token, "GetCustomer", Customer_id);

    response.then(function (query) {
        if (query.startsWith("Error:")) {
            ShowDialog("Alert", query);
            return;
        }
        let responce_query = JSON.parse(query);

        if (responce_query.result.startsWith("Error:")) {
            ShowDialog("Alert", responce_query.result);
            return;
        }

        if (responce_query.result == "[]") {
            ShowDialog("Alert", "No Customer found: " + Sku_id);
            textSku.value = "";
            return;
        }

        localCustomer = JSON.parse(responce_query.result);
        //document.getElementById("customer_text").innerText = "Customer (F8): " + localCustomer.Customer_id + " " + localCustomer.Customer_name;
        customer_text.innerText = "Customer (F8): " + localCustomer.Customer_id + " " + localCustomer.Customer_name;

    });
}


function ShowItemEditDialog(SaleDetail_id) {

    var sale_detail = undefined;

    for (let i = 0; i < sale.Sale_detail.length; i++) {
        if (sale.Sale_detail[i].Id == SaleDetail_id) {
            sale_detail = sale.Sale_detail[i];
            break;
        }
    }

    if (sale_detail == undefined) {
        ShowDialog("Alert", "Item not found: " + SaleDetail_id);
        return;
    }

    item_to_edit = SaleDetail_id;

    document.getElementById("sku_item").value = sale_detail.Sku_id;
    document.getElementById("sku_item").disabled = true;
    document.getElementById("sku_item_description").value = sale_detail.Description;
    document.getElementById("sku_item_qty").value = sale_detail.Qty;
    document.getElementById("sku_item_price").value = sale_detail.Price_with_taxes;
    document.getElementById("sku_item_description").focus();
    document.getElementById("dialog_sku_edit").showModal();

}

function AddItem(Sku_id) {

    //document.getElementById("textSku").disabled = true;

    var response = sendToAngelPOST(Token.User, "pos_backend/pos_skus", Token.Token, "GetSku", Sku_id);

    response.then(function (query) {

        if (query.startsWith("Error:")) {
            ShowDialog("Alert", query);
            return;
        }
        let responce_query = JSON.parse(query);

        if (responce_query.result.startsWith("Error:")) {
            ShowDialog("Alert", responce_query.result);
            return;
        }

        if (responce_query.result == "[]") {
            ShowDialog("Alert", "No Sku found: " + Sku_id);
            textSku.value = "";
            return;
        }

        if (typeof sale === 'undefined') {
            sale = new Sale();
            sale.Id = generateGUID();
            sale.DateTime = formatDate(new Date());
            sale.Receipt_serie = null;
            sale.Receipt_number = null;
            sale.Sale_type = "POS";

            sale.Currency_id = default_currency.Id;
            sale.Currency_name = default_currency.Description;
            sale.Exchange_rate = 1;

            sale.Total = 0;
            sale.Subtotal = 0;
            sale.Cost = 0;
            sale.Consumption_tax = 0;
            sale.For_credit = 0;
            sale.ReferenceID = "";
            sale.ReferenceType = "";
            sale.User_id = Token.User;
            sale.User_name = Token.User;
            sale.Customer_id = "SYS";
            sale.Customer_name = "";
            sale.Seller_id = Token.User;
            sale.Seller_name = Token.User;
            sale.POS_ID = "WEB_POS";
            sale.Account_id = "";
            sale.Location = "";
            sale.Business_data = "";
            sale.Number_of_items = 0;
        }

        let sku = JSON.parse(responce_query.result);

        let sale_detail = new SaleDetail();
        sale_detail.Id = generateGUID();
        sale_detail.Sale_id = sale.Id;
        sale_detail.Sku_id = sku.id;
        sale_detail.Description = sku.Description;
        sale_detail.Qty = 1;
        sale_detail.Price = sku.Price;
        sale_detail.Original_price = sku.Price;
        sale_detail.Discount = 0;

        let impuestos = JSON.parse(sku.Consumption_taxes);

        sale_detail.Price_with_taxes = sku.Price;
        sale_detail.Price_with_taxes = parseFloat(sale_detail.Price_with_taxes).toFixed(6);
        sale_detail.Consumption_tax = 0;

        let tax_percent = 0;

        // sku price includes taxes, we need to calculate the base price
        sale_detail.Price_with_taxes = Round(sku.Price, 6);
        sale_detail.Price = Round(sku.Price, 6);

        for (let j = 0; j < impuestos.length; j++) {
            tax_percent += impuestos[j].Rate;
            sale_detail.Price = sale_detail.Price / (1 + (impuestos[j].Rate / 100));
        }

        sale_detail.Price = Round(sale_detail.Price, 6);
        sale_detail.Consumption_tax = sale_detail.Price_with_taxes - sale_detail.Price;
        sale_detail.Consumption_tax = Round(sale_detail.Consumption_tax, 6);
        sale_detail.Import = sale_detail.Price_with_taxes;
        sale_detail.Cost = Round(sku.Cost, 6);
        sale_detail.Consumption_tax_percentages = tax_percent;
        sale_detail.DateTime = formatDate(new Date());
        sale_detail.User_id = Token.User;
        sale_detail.Sku_dictionary_id = "";
        sale_detail.Qty_equivalence = 1;
        sale_detail.Description_equivalence = "";
        sale_detail.ClaveProdServ = sku.ClaveProdServ;
        sale_detail.ClaveUnidad = sku.ClaveUnidad;
        sale_detail.Observations = "";
        sale_detail.Preferential_Classification = "";
        //sale_detail.Classifications = JSON.parse(sku.SkuClassification);
        sale_detail.Consumption_taxes = JSON.parse(sku.Consumption_taxes);

        sale.Sale_detail.push(sale_detail);

        sale.Total = 0;
        sale.Subtotal = 0;
        sale.Cost = 0;
        sale.Number_of_items = 0;
        sale.Consumption_tax = 0;

        var detail_zone = document.getElementById("sale_detail");
        //detail_zone.innerHTML = "";

        if (sku.Image != undefined && sku.Image != "") {
            let image = document.getElementById("sku_image");
            image.src = sku.Image;
            image.style.width = "140px";
            //image.style.height = "120px";

            document.getElementById("sku_description").innerHTML = sku.Description;

        }
        else {
            let image = document.getElementById("sku_image");
            image.src = "images/Kiosko_logo.png";
            image.style.width = "140px";
            //image.style.height = "120px";

            document.getElementById("sku_description").innerHTML = sku.Description;

        }

        let button = document.createElement("button");

        if (sale_detail_style == 0) {
            button.className = "btn btn-success form-control w-100 h-200 d-flex align-items-center"; // <- alineación horizontal
            button.style = "margin-bottom: 5px; margin-top: 5px; height: 60px; background-color: azure; color: black;";
            sale_detail_style = 1;
        } else {
            button.className = "btn btn-primary form-control w-100 h-200 d-flex align-items-center";
            button.style = "margin-bottom: 5px; margin-top: 5px; height: 60px; background-color: #d1e9f0; color: black;";
            sale_detail_style = 0;
        }

        button.id = sale_detail.Id;

        // Imagen base64 opcional
        let imgTag = "";
        if (sku.Image && sku.Image != "") {
            imgTag = `<img src="${sku.Image}" style="height:40px; width:auto; margin-right:10px;">`;
        }

        let text = `<div class="text-start">
            <strong>${sale_detail.Sku_id} ${sale_detail.Description}<br>
            Qty: ${sale_detail.Qty} Price: $${ThousandsSeparator(sale_detail.Price_with_taxes)} Subtotal: $${ThousandsSeparator(sale_detail.Import)}</strong>
        </div>`;

        button.innerHTML = imgTag + text;

        button.addEventListener("click", function () {
            ShowItemEditDialog(sale_detail.Id);
        });

        detail_zone.prepend(button);
        search_result.close();

        for (let i = 0; i < sale.Sale_detail.length; i++) {
            sale.Total += sale.Sale_detail[i].Price_with_taxes;
            sale.Subtotal += sale.Sale_detail[i].Price;
            sale.Cost += sale.Sale_detail[i].Cost;
            sale.Number_of_items += 1;
            sale.Consumption_tax += sale.Sale_detail[i].Consumption_tax;
        }

        sale.Total = Round(parseFloat(sale.Total), 6);
        sale.Subtotal = Round(parseFloat(sale.Subtotal), 6);
        sale.Cost = Round(parseFloat(sale.Cost), 6);
        sale.Number_of_items = Round(parseFloat(sale.Number_of_items), 6);
        sale.Consumption_tax = Round(parseFloat(sale.Consumption_tax), 6);

        btnItems_text.innerHTML = "Items " + sale.Number_of_items;
        btnTotal_text.innerHTML = "Total $" + ThousandsSeparator(sale.Total);

    }).finally(function () {
        // Habilitar el campo de texto después de la respuesta
        document.getElementById("textSku").value = "";
        document.getElementById("textSku").disabled = false;
        document.getElementById("textSku").focus();
    });

}



function DeleteItem() {

    if (item_to_edit == undefined) {
        ShowDialog("Alert", "No sale item found");
        return;
    }

    var sale_detail_index = -1;

    for (let i = 0; i < sale.Sale_detail.length; i++) {
        if (sale.Sale_detail[i].Id == item_to_edit) {
            sale_detail_index = i;
            break;
        }
    }

    if (sale_detail_index == -1) {
        ShowDialog("Alert", "Item not found");
        return;
    }

    ShowAcceptCancelDialog("Delete Item", "Are you sure you want to delete this item?", function () {

        sale.Total = 0;
        sale.Subtotal = 0;
        sale.Cost = 0;
        sale.Number_of_items = 0;
        sale.Consumption_tax = 0;

        let button = document.getElementById(sale.Sale_detail[sale_detail_index].Id);
        button.remove();

        sale.Sale_detail.splice(sale_detail_index, 1);

        for (let i = 0; i < sale.Sale_detail.length; i++) {
            sale.Total += Number(sale.Sale_detail[i].Price_with_taxes) * Number(sale.Sale_detail[i].Qty);
            sale.Subtotal += Number(sale.Sale_detail[i].Price) * Number(sale.Sale_detail[i].Qty);
            sale.Cost += Number(sale.Sale_detail[i].Cost);
            sale.Number_of_items += Number(sale.Sale_detail[i].Qty);
            sale.Consumption_tax += Number(sale.Sale_detail[i].Consumption_tax) * Number(sale.Sale_detail[i].Qty);
        }

        btnItems_text.innerHTML = "Items " + sale.Number_of_items;
        btnTotal_text.innerHTML = "Total $" + ThousandsSeparator(sale.Total);

        ShowAcceptCancelDialogClose();
        dialog_sku_edit.close();
        textSku.focus();

    });


}



function SaveItem() {
    var SaleDetail_id = item_to_edit;
    var sale_detail = undefined;

    for (let i = 0; i < sale.Sale_detail.length; i++) {
        if (sale.Sale_detail[i].Id == SaleDetail_id) {
            sale_detail = sale.Sale_detail[i];
            break;
        }
    }

    if (sale_detail == undefined) {
        ShowDialog("Alert", "Item not found: " + SaleDetail_id);
        return;
    }

    sale_detail.Description = document.getElementById("sku_item_description").value;

    var qty = Number(document.getElementById("sku_item_qty").value);

    if (qty <= 0) {
        ShowDialog("Alert", "Quantity must be greater than zero");
        return;
    }

    sale_detail.Qty = qty;

    var price = Number(document.getElementById("sku_item_price").value);

    if (price <= 0) {
        ShowDialog("Alert", "Price must be greater than zero");
        return;
    }

    sale_detail.Price_with_taxes = Number(price);
    sale_detail.Import = Number(sale_detail.Price_with_taxes) * Number(sale_detail.Qty);
    sale_detail.Price = Number(sale_detail.Price_with_taxes) / (1 + (Number(sale_detail.Consumption_tax_percentages) / 100));
    sale_detail.Consumption_tax = Number(sale_detail.Price_with_taxes) - Number(sale_detail.Price);

    sale.Total = 0;
    sale.Subtotal = 0;
    sale.Cost = 0;
    sale.Number_of_items = 0;
    sale.Consumption_tax = 0;

    for (let i = 0; i < sale.Sale_detail.length; i++) {
        sale.Total += Number(sale.Sale_detail[i].Price_with_taxes) * Number(sale.Sale_detail[i].Qty);
        sale.Subtotal += Number(sale.Sale_detail[i].Price) * Number(sale.Sale_detail[i].Qty);
        sale.Cost += Number(sale.Sale_detail[i].Cost);
        sale.Number_of_items += Number(sale.Sale_detail[i].Qty);
        sale.Consumption_tax += Number(sale.Sale_detail[i].Consumption_tax) * Number(sale.Sale_detail[i].Qty);
    }

    btnItems_text.innerHTML = "Items " + sale.Number_of_items;
    btnTotal_text.innerHTML = "Total $" + sale.Total.toFixed(2);
    textSku.value = "";

    let button = document.getElementById(SaleDetail_id);
    button.innerHTML = sale_detail.Sku_id + " " + sale_detail.Description + "<br>Qty: " + sale_detail.Qty + " Price: $" + sale_detail.Price_with_taxes.toFixed(2) + " Subtotal: $" + sale_detail.Import.toFixed(2);

    dialog_sku_edit.close();
    textSku.focus();

}


function SavePaymentMethod() {

    var payment_method = document.getElementById("payment_method").value;

    if (payment_method == "") {
        ShowDialog("Alert", "Payment method is empty");
        return;
    }

    if (Number(document.getElementById("pay").value) <= 0) {
        ShowDialog("Alert", "Payment amount must be greater than zero");
        return;
    }

    if (payments == undefined) {
        payments = [];
    }

    var payment = new Payments();
    payment.id = generateGUID();
    payment.Sale_id = sale.Id;
    payment.Description = document.getElementById("payment_comments").value;
    payment.Type = payment_method;
    payment.ReferenceID = "";
    payment.ReferenceType = "";
    payment.DateTime = formatDate(new Date());
    payment.User_id = sale.User_id;
    payment.Account_id = sale.Account_id;
    payment.User_name = sale.User_name;
    payment.Amount = Number(document.getElementById("pay").value);
    payment.DateTime = formatDate(new Date());
    payment.Currency_id = document.getElementById("currency_id").value;
    payment.Exchange_rate = Number(document.getElementById("exchange_rate").value);
    payments.push(payment);

    sale.Payments = payments;

    ShowPayments();

}


function ShowPayments() {
    localpayments = [];

    for (let i = 0; i < payments.length; i++) {
        var localpayment = new LocalPayments();
        var id = i + 1;
        localpayment.id = id.toString();
        localpayment.Payment_id = payments[i].id;
        localpayment.Amount = payments[i].Amount;
        localpayment.Description = payments[i].Description;
        localpayment.Currency_id = payments[i].Currency_id;
        localpayment.Exchange_rate = payments[i].Exchange_rate;
        localpayment.Type = payments[i].Type;
        localpayments.push(localpayment);
    }

    const customCols =
    {
        "id":
        {
            "title": "ID",
            "html": (value) => `<button class="btn btn-success" style="width:100%"><strong>${value}</strong></button>`,
            "onclick": (val, row) => DeletePayment(`${row.Payment_id}`),
            "style": "color: blue; cursor: pointer;"
        },
        "Payment_id":
        {
            "visible": false
        },
    };

    renderPaginatedTable(localpayments, "tableContainer", 20, customCols, null, false);

    CalculatePayment();

    if (sale.Change >= 0) {
        document.getElementById("pay").value = "";
        document.getElementById("dialog_confirm_accept").focus();
    }
    else {
        document.getElementById("pay").value = "";
        document.getElementById("pay").focus();
    }

}


function DeletePayment(Payment_id) {

    ShowAcceptCancelDialog("Delete Payment", "Are you sure you want to delete this payment?", function () {
        document.getElementById("dialog_accept").close();
        //document.getElementById("dialog_sale_confirm").close();
        payments = payments.filter(payment => payment.id !== Payment_id);
        sale.Payments = payments;

        document.getElementById("pay").focus();

        if (payments.length == 0) {
            document.getElementById("tableContainer").innerHTML = "";
            return;
        }

        ShowPayments();
    });

}


function CalculatePayment() {
    var total = Number(sale.Total);
    var remaining = 0;

    var total_payments = 0;

    if (payments == undefined) {
        total_payments = 0;
    }
    else {
        for (let i = 0; i < payments.length; i++) {
            total_payments += Number(payments[i].Amount) * Number(payments[i].Exchange_rate);
        }
    }

    sale.Total_payments = total_payments;

    remaining = Number(total) - Number(total_payments);

    if (remaining > 0) {
        sale.Change = remaining * -1;
        document.getElementById("total_confirm").innerHTML = "<strong>Total: $" + ThousandsSeparator(total) + "</strong>  Remaining: $" + ThousandsSeparator(remaining);
    }
    else {
        sale.Change = Math.abs(remaining);
        document.getElementById("total_confirm").innerHTML = "<strong>Total: $" + ThousandsSeparator(total) + "</strong>  Change: $" + ThousandsSeparator(sale.Change);
    }

}


function SaveSale(credit = false) {

    CalculatePayment();

    if (credit == false) {

        if (sale.Total_payments < sale.Total) {

            ShowAcceptCancelDialog("Alert", "Do you want the sale to be on credit?", function () {
                document.getElementById("btnConfirmSale").disabled = true;
                document.getElementById("dialog_accept").close();
                document.getElementById("dialog_sale_confirm").close();
                sale.For_credit = 1;
                SaveSale(true);
            });

            return;

        }
        else {
            sale.For_credit = 0;
        }

    }
    else {
        if (sale.Total_payments >= sale.Total) {
            ShowDialog("Alert", "The sale is already paid in full. You cannot save it as a credit sale.");
            return;
        }
    }


    // localCustomer.Customer_id + " " + localCustomer.Customer_name
    if (localCustomer != null) {
        sale.customer_id = localCustomer.Customer_id;
        sale.customer_name = localCustomer.Customer_name;
    }

    var response = sendToAngelPOST(Token.User, "pos_backend/pos_backend", Token.Token, "UpsertSale", sale);

    response.then(function (query) {

        document.getElementById("dialog_confirm_accept").disabled = true;

        if (query.startsWith("Error:")) {
            ShowDialog("Alert", query);
            return;
        }
        let responce_query = JSON.parse(query);

        if (responce_query.result.startsWith("Error:")) {
            ShowDialog("Alert", responce_query.result);
            return;
        }

        //ShowDialog("Ok.", "Sale saved successfully");

        var account = "";
        if (Token.User.includes("@")) {
            account = Token.User.split("@")[1];
        }

        var sale_id = sale.Id;

        localCustomer = undefined;
        sale_detail = undefined;
        sale = undefined;
        payments = undefined;

        document.getElementById("btnItems").innerText = "Items 0";
        document.getElementById("btnTotal").innerText = "Total $0.00";
        document.getElementById("sale_detail").innerHTML = "";
        document.getElementById("customer_text").innerText = "Customer (F8): SYS";
        document.getElementById("pay").value = "";
        document.getElementById("payment_method").value = "cash";
        document.getElementById("payment_comments").value = "";

        document.getElementById("tableContainer").innerHTML = "";
        //document.getElementById("tableContainer").style.display = "none";

        document.getElementById("sku_description").src = businessinfo.Description;
        let image = document.getElementById("sku_image");
        image.src = businessinfo.Logo;
        image.style.width = "140px";

        if (typeof clientKey !== 'undefined' && clientKey) {
            url = window.location.protocol + '//' + window.location.host + "/" + clientKey + "/kiosko/";
        }
        else {
            url = window.location.protocol + '//' + window.location.host + "/kiosko/";
        }

        document.getElementById("dialog_sale_confirm").close();
        document.getElementById("my_frame").src = url + `ticket.html?account=${account}&sale_id=${sale_id}`;
        document.getElementById("textSku").focus();

    }).finally(function () {
        // Habilitar el botón después de la respuesta
        document.getElementById("dialog_confirm_accept").disabled = false;
    });

}

window.onload = async function () {

    if (sessionStorage.getItem("Token") == null) {
        window.location.href = "index.html";
        return;
    }

    language = getSelectedLanguage();
    Token = JSON.parse(sessionStorage.getItem("Token"));

    textSku = document.getElementById("textSku");
    btnItems_text = document.getElementById("btnItems_text");
    btnTotal_text = document.getElementById("btnTotal_text");
    sale_detail = document.getElementById("sale_detail");
    search_result = document.getElementById("search_result");
    searchcustomer_result = document.getElementById("searchcustomer_result");
    customer_text = document.getElementById("customer_text");
    dialog_customer = document.getElementById("dialog_customer");
    customer_dialog_button_close = document.getElementById("customer_dialog_button_close");
    customer_dialog_button_accept = document.getElementById("customer_dialog_button_accept");
    btnSearchCustomer = document.getElementById("btnSearchCustomer");
    btnNewCustomer = document.getElementById("btnNewCustomer");

    dialog_sku_edit = document.getElementById("dialog_sku_edit");
    dialog_skuedit_close = document.getElementById("dialog_skuedit_close");

    btnItems_text.innerHTML = "Items 0";
    btnTotal_text.innerHTML = "Total $0.00";

    btnConfirmSale = document.getElementById("btnConfirmSale");

    customer_text.innerText = "Customer (F8): SYS General public";

    textSku.addEventListener("keydown", function (event) {
        if (event.key === 'Enter') {
            if (document.getElementById("textSku").value == "") return;
            var sku_id = document.getElementById("textSku").value;
            document.getElementById("textSku").value = "";
            event.preventDefault();
            AddItem(sku_id);
        }

        if (event.key === 'ArrowDown') {
            event.preventDefault();
            SearchSkus();
        }
    });

    document.getElementById("btnSearchSku").addEventListener("click", async function () {
        SearchSkus();
    });

    var dialog_button_close = document.getElementById("dialog_button_close");

    dialog_button_close.addEventListener("click", function () {
        CloseDialog();
    });

    var dialog_button_close1 = document.getElementById("dialog_button_close1");

    dialog_button_close1.addEventListener("click", function () {
        ShowAcceptCancelDialogClose();
    });

    document.addEventListener('keydown', function (event) {
        if (event.key === 'F2') {
            event.preventDefault();
            textSku.focus();
        }
        if (event.key === 'F8') {
            event.preventDefault();
            ShowCustomerDialog();
        }

        if (event.key === 'F3') {
            event.preventDefault();
            SearchSkus();
        }

        if (event.key === 'F4') {
            event.preventDefault();
            SearchCustomers();
        }

        if (event.key === 'F7') {
            event.preventDefault();
            ShowConfirmDialog();
        }

        if (event.key === 'F5') {
            event.preventDefault();
            ShowConfirmDialog();
        }

    });

    document.getElementById("btnCustomer").addEventListener("click", function () {
        ShowCustomerDialog();
    });

    customer_dialog_button_close.addEventListener("click", function () {
        dialog_customer.close();
    });

    btnSearchCustomer.addEventListener("click", function () {
        SearchCustomers();
    });

    customer_dialog_button_accept.addEventListener("click", function () {
        SaveCustomer();
    });

    btnNewCustomer.addEventListener("click", function () {
        NewCustomer();
    });

    dialog_skuedit_close.addEventListener("click", function () {
        dialog_sku_edit.close();
    });

    dialog_skuedit_accept.addEventListener("click", function () {
        SaveItem();
    });

    btnConfirmSale.addEventListener("click", function () {
        ShowConfirmDialog();
    });

    document.getElementById("dialog_confirm_accept").addEventListener("click", function () {
        SaveSale(false);
    });

    document.getElementById("dialog_confirm_cancel").addEventListener("click", function () {
        document.getElementById("dialog_sale_confirm").close();
    });


    document.getElementById("dialog_skuedit_delete").addEventListener("click", function () {
        DeleteItem();
    });


    document.querySelectorAll('input[type="text"]').forEach(input => {
        input.addEventListener('focus', function () {
            this.select();
        });
    });

    document.getElementById("pay").addEventListener("input", function () {
        CalculatePayment();
    });

    document.getElementById("dialog_sale_confirm").addEventListener("close", function () {
        document.getElementById("pay").value = "";
    });

    document.getElementById("pay").addEventListener("keydown", function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            SavePaymentMethod();
        }
    });

    document.getElementById("dialog_credit_accept").addEventListener("click", function () {

        ShowAcceptCancelDialog("Alert", "Do you want the sale to be on credit?", function () {
            document.getElementById("dialog_accept").close();
            document.getElementById("dialog_sale_confirm").close();
            sale.For_credit = 1;
            SaveSale(true);
        });

    });

    document.getElementById("btnConfirmSale").addEventListener("click", function () {
        if (sale == undefined) {
            ShowDialog("Alert", "No items in the sale");
            return;
        }

        if (sale.Sale_detail.length == 0) {
            ShowDialog("Alert", "No items in the sale");
            return;
        }

        document.getElementById("dialog_sale_confirm").showModal();
    });

    document.getElementById("button_accept").addEventListener("click", function () {
        SavePaymentMethod();
    });

    document.getElementById("button_cash").addEventListener("click", function () {
        document.getElementById("payment_method").value = "cash";
        SavePaymentMethod();
    });

    document.getElementById("button_credit_card").addEventListener("click", function () {
        document.getElementById("payment_method").value = "credit_card";
        SavePaymentMethod();
    });

    document.getElementById("button_transfer").addEventListener("click", function () {
        document.getElementById("payment_method").value = "transfer";
        SavePaymentMethod();
    });


    GetBusinessLines();
    businessinfo = await GetBusinessInfo();
    document.getElementById("sku_description").innerText = businessinfo.Slogan;
    let image = document.getElementById("sku_image");
    image.src = businessinfo.Logo;
    image.style.width = "140px";

    default_currency = await GetDefaultCurrency();
    await GetCurrencies();

    document.getElementById("pay").onkeydown = function (event) {
        if (event.key === 'Enter') {
            event.preventDefault();
            document.getElementById("btnConfirmSale").focus();
        }
    }

    document.getElementById("button_accept").addEventListener("click", function () {
        SavePaymentMethod();
    });


    textSku.focus();

}



// Sale class in JavaScript
class Sale {
    constructor() {
        this.Id = "";
        this.DateTime = "";
        this.Receipt_serie = "";
        this.Receipt_number = "";
        this.Sale_type = "";
        this.Currency_id = "";
        this.Currency_name = "";
        this.Exchange_rate = 0;
        this.Total = 0;
        this.Subtotal = 0;
        this.Cost = 0;
        this.Consumption_tax = 0;
        this.For_credit = 0;
        this.ReferenceID = "";
        this.ReferenceType = "";
        this.User_id = "";
        this.User_name = "";
        this.Customer_id = "";
        this.Customer_name = "";
        this.Seller_id = "";
        this.Seller_name = "";
        this.POS_ID = "";
        this.Account_id = "";
        this.Location = "";
        this.Business_data = "";
        this.Number_of_items = 0;
        this.Change = 0;
        this.Total_payments = 0;
        this.Sale_detail = []; // List of Sale_detail objects
        this.Payments = []; // List of Payment_method objects
    }
}


// Sale_detail class in JavaScript with PascalCase
class SaleDetail {
    constructor() {
        this.Id = "";
        this.Sale_id = "";
        this.Sku_id = "";
        this.Description = "";
        this.Qty = 0;
        this.Price = 0;
        this.Original_price = 0;
        this.Discount = 0;
        this.Import = 0;
        this.Cost = 0;
        this.Consumption_tax_percentages = 0;
        this.Consumption_tax = 0;
        this.Price_with_taxes = 0;
        this.PromotionCode = "";
        this.DateTime = "";
        this.User_id = "";
        this.Sku_dictionary_id = "";
        this.Qty_equivalence = 0;
        this.Description_equivalence = "";
        this.ClaveProdServ = "";
        this.ClaveUnidad = "";
        this.Observations = "";
        this.Preferential_Classification = "";
        this.Classifications = []; // List of Classification objects
        this.Consumption_taxes = []; // List of ConsumptionTax objects
    }
}



/**
 * Class used to store the customer information
 */
class Customer {
    Customer_id;                    // Customer identifier
    Customer_name;                  // Customer name
    RFC;                   // Customer RFC
    CP;                    // Customer postal code
    Email;                 // Customer email
    Phone;                 // Customer phone
    Address;               // Customer address
    City;                  // Customer city
    State;                 // Customer state
    Country;               // Customer country
    DateTinme;             // Customer creation date
    User_id;               // User identifier
    Currency_id;           // Currency identifier
    BusinessLine_id;       // Business line identifier
    BusinessLine_description; // Business line description
}

// Payment methods used in each sale
class Payments {
    id;
    Sale_id;
    Account_id;
    Description;
    Type;
    ReferenceType;
    ReferenceID;
    DateTime;
    Amount;
    Currency_id;
    exchange_rate;
}

// Payment methods used in each sale
class LocalPayments {
    id;
    Payment_id;
    Description;
    Type;
    Amount;
    Currency_id;
    Exchange_rate;  // Instance of Currency
}

/**
 * BusinessInfo class, used to store the business information
 */
class BusinessInfo {
    // Business identifier
    id;
    // Business Address
    Address;
    // Business Name
    Name;
    // Business Phone
    Phone;
    // Business Email
    Email;
    // Business Website
    Website;
    // Business Logo
    Logo;
    // Business Slogan
    Slogan;
    // Currency ID
    Currency_id;
    // Currency Name
    Currency_name;
}