/**
 * Downloads a file from base64 encoded data
 * @param {string} fileName - Name to give the downloaded file
 * @param {string} base64Data - Base64 encoded file data
 * @param {string} contentType - MIME type of the file
 */
window.downloadFileFromBase64 = (fileName, base64Data, contentType) => {
    // Create a blob from the base64 data
    const byteCharacters = atob(base64Data);
    const byteArrays = [];

    for (let offset = 0; offset < byteCharacters.length; offset += 512) {
        const slice = byteCharacters.slice(offset, offset + 512);

        const byteNumbers = new Array(slice.length);
        for (let i = 0; i < slice.length; i++) {
            byteNumbers[i] = slice.charCodeAt(i);
        }

        const byteArray = new Uint8Array(byteNumbers);
        byteArrays.push(byteArray);
    }

    const blob = new Blob(byteArrays, { type: contentType });

    // Create download link and trigger click
    const url = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = fileName;

    // Append to body, click and remove
    document.body.appendChild(link);
    link.click();

    // Clean up
    setTimeout(() => {
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
    }, 100);
};

/**
 * Prints a PDF from base64 encoded data
 * @param {string} base64Data - Base64 encoded PDF data
 */
window.printPdfFromBase64 = (base64Data) => {
    // Create a blob from the base64 data
    const byteCharacters = atob(base64Data);
    const byteArrays = [];

    for (let offset = 0; offset < byteCharacters.length; offset += 512) {
        const slice = byteCharacters.slice(offset, offset + 512);

        const byteNumbers = new Array(slice.length);
        for (let i = 0; i < slice.length; i++) {
            byteNumbers[i] = slice.charCodeAt(i);
        }

        const byteArray = new Uint8Array(byteNumbers);
        byteArrays.push(byteArray);
    }

    const blob = new Blob(byteArrays, { type: 'application/pdf' });
    const url = window.URL.createObjectURL(blob);

    // Create an iframe for printing
    const printFrame = document.createElement('iframe');
    printFrame.style.display = 'none';
    printFrame.src = url;

    printFrame.onload = () => {
        try {
            printFrame.contentWindow.print();
        } catch (error) {
            console.error('Error printing PDF:', error);
        } finally {
            // Clean up after printing
            setTimeout(() => {
                document.body.removeChild(printFrame);
                window.URL.revokeObjectURL(url);
            }, 1000);
        }
    };

    document.body.appendChild(printFrame);
};

/**
 * Gets the outer HTML of an element by id for PDF generation
 * @param {string} elementId
 * @returns {string}
 */
window.getHtmlForPdf = function(elementId) {
    var el = document.getElementById(elementId);
    if (!el) return '';
    // Clone the node to avoid modifying the live DOM
    var clone = el.cloneNode(true);
    // Optionally, inline styles or add base tag here
    // Wrap in a full HTML document for DinkToPdf
    var docHtml = `<!DOCTYPE html><html><head>` +
        document.head.innerHTML +
        `</head><body>` + clone.outerHTML + `</body></html>`;
    return docHtml;
};