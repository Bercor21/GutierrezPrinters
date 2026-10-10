// Gutierrez Printers - JavaScript del sitio (sin librerías extra, salvo el generador de QR).

(function () {
    "use strict";

    // ---------- 1. Dibujar códigos QR ----------
    // Cualquier elemento con data-qr="texto" se convierte en un QR (SVG).
    function renderQrCodes() {
        if (typeof window.qrcode !== "function") {
            return;
        }
        document.querySelectorAll("[data-qr]").forEach(function (el) {
            var qr = window.qrcode(0, "M");
            qr.addData(el.getAttribute("data-qr"));
            qr.make();
            el.innerHTML = qr.createSvgTag({ cellSize: 4, margin: 0, scalable: true });
        });
    }

    // ---------- 2. Formulario de ingreso ----------
    function setupNewOrderForm() {
        var form = document.getElementById("new-order-form");
        if (!form) {
            return;
        }

        var lookupUrl = form.getAttribute("data-lookup-url");
        var phone = document.getElementById("CustomerPhone");
        var customerId = document.getElementById("CustomerId");
        var name = document.getElementById("CustomerName");
        var email = document.getElementById("CustomerEmail");
        var idNumber = document.getElementById("CustomerIdNumber");
        var hint = document.getElementById("customer-lookup-hint");
        var printerGroup = document.getElementById("existing-printer-group");
        var printerSelect = document.getElementById("PrinterId");
        var newPrinterFields = document.getElementById("new-printer-fields");
        var lastLookup = "";
        var timer = null;

        function digitsOf(text) {
            return (text || "").replace(/\D/g, "");
        }

        // Si se escogió una impresora que ya vino, se esconden los campos de impresora nueva.
        function toggleNewPrinterFields() {
            var usingExisting = printerSelect && printerSelect.value !== "";
            newPrinterFields.classList.toggle("d-none", usingExisting);
        }

        function setPrinters(printers) {
            // Deja solo la primera opción ("No, es otra impresora") y agrega las del cliente.
            while (printerSelect.options.length > 1) {
                printerSelect.remove(1);
            }
            (printers || []).forEach(function (p) {
                var option = document.createElement("option");
                option.value = p.id;
                option.textContent = p.label;
                printerSelect.appendChild(option);
            });
            printerSelect.value = "";
            printerGroup.classList.toggle("d-none", !printers || printers.length === 0);
            toggleNewPrinterFields();
        }

        function showHint(text, css) {
            hint.textContent = text;
            hint.className = "lookup-hint form-text " + (css || "");
        }

        function lookup() {
            var digits = digitsOf(phone.value);
            if (digits.length < 8) {
                if (customerId.value) {
                    customerId.value = "";
                    setPrinters([]);
                }
                showHint("Si el cliente ya vino antes, sus datos se llenan solos.");
                lastLookup = "";
                return;
            }
            if (digits === lastLookup) {
                return;
            }
            lastLookup = digits;

            fetch(lookupUrl + "?phone=" + encodeURIComponent(digits), { headers: { "Accept": "application/json" } })
                .then(function (response) { return response.ok ? response.json() : { found: false }; })
                .then(function (data) {
                    if (data.found) {
                        customerId.value = data.id;
                        name.value = data.name || "";
                        email.value = data.email || "";
                        idNumber.value = data.idNumber || "";
                        setPrinters(data.printers);
                        showHint("Cliente encontrado: " + data.name + ". Revisá que los datos estén bien.", "text-success fw-semibold");
                    } else {
                        customerId.value = "";
                        setPrinters([]);
                        showHint("Cliente nuevo: llená su nombre.", "text-primary");
                    }
                })
                .catch(function () {
                    showHint("No se pudo buscar el cliente. Podés seguir llenando los datos.", "text-danger");
                });
        }

        phone.addEventListener("input", function () {
            clearTimeout(timer);
            timer = setTimeout(lookup, 400);
        });
        phone.addEventListener("blur", lookup);

        if (printerSelect) {
            printerSelect.addEventListener("change", toggleNewPrinterFields);
            toggleNewPrinterFields();
        }

        if (customerId.value) {
            lastLookup = digitsOf(phone.value);
            showHint("Cliente registrado.", "text-success fw-semibold");
        }
    }

    document.addEventListener("DOMContentLoaded", function () {
        renderQrCodes();
        setupNewOrderForm();
    });
})();
