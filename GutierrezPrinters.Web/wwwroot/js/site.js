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

    // ---------- 3. Boleta: nunca más de una hoja carta ----------
    // El contenido de [data-fit-page="ANCHOxALTO"] (área imprimible en mm) se achica solo lo
    // necesario para que quepa en una hoja. Si ya cabe, queda en su tamaño normal.
    function fitToPage() {
        var box = document.querySelector("[data-fit-page]");
        if (!box || !box.firstElementChild) {
            return;
        }
        var content = box.firstElementChild;
        var size = box.getAttribute("data-fit-page").split("x");
        var maxRatio = parseFloat(size[1]) / parseFloat(size[0]);
        var note = document.querySelector("[data-fit-note]");

        // Se compara alto/ancho, así no importa si la hoja se ve achicada en el celular.
        function fits() {
            var rect = box.getBoundingClientRect();
            return rect.width > 0 && rect.height / rect.width <= maxRatio * 0.985;
        }

        content.style.zoom = "";
        var zoom = 1;
        if (!fits() && "zoom" in content.style) {
            var low = 0.25;
            var high = 1;
            for (var i = 0; i < 12; i++) {
                var mid = (low + high) / 2;
                content.style.zoom = mid;
                if (fits()) {
                    low = mid;
                } else {
                    high = mid;
                }
            }
            zoom = low;
            content.style.zoom = zoom;
        }
        if (note) {
            note.hidden = zoom >= 1;
        }
    }

    // En el celular la hoja completa se ve a lo ancho de la pantalla (solo en pantalla).
    function fitPaperToScreen() {
        var paper = document.querySelector(".paper");
        if (!paper) {
            return;
        }
        var root = document.documentElement;
        root.style.removeProperty("--screen-zoom");
        var available = root.clientWidth - 16;
        var zoom = Math.min(1, available / paper.offsetWidth);
        root.style.setProperty("--screen-zoom", zoom.toFixed(3));
    }

    function setupPrintPage() {
        if (!document.querySelector("[data-fit-page]")) {
            return;
        }
        fitPaperToScreen();
        fitToPage();
        // Cuando termina de cargar la letra, se vuelve a medir.
        if (document.fonts && document.fonts.ready) {
            document.fonts.ready.then(fitToPage);
        }
        // Se mide en pantalla, donde el ancho es igual al de la hoja impresa. No se vuelve a medir
        // al imprimir: en ese momento el navegador puede dar medidas que no sirven.
        window.addEventListener("load", fitToPage);
        window.addEventListener("resize", fitPaperToScreen);
    }

    // ---------- 4. Confirmar antes de enviar ----------
    // Un <form data-confirm="¿Seguro?"> pregunta antes de enviarse (por ejemplo, al eliminar).
    document.addEventListener("submit", function (event) {
        var message = event.target.getAttribute && event.target.getAttribute("data-confirm");
        if (message && !window.confirm(message)) {
            event.preventDefault();
        }
    });

    document.addEventListener("DOMContentLoaded", function () {
        renderQrCodes();
        setupNewOrderForm();
        setupPrintPage();
    });
})();
