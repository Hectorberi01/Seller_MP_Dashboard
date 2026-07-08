// Téléchargement côté client d'un contenu texte (ex. export CSV des finances).
// Ajoute un BOM UTF-8 pour que les accents s'affichent correctement dans Excel FR.
window.hbaDownload = (filename, text, mime) => {
    try {
        const type = (mime || "text/csv") + ";charset=utf-8";
        const bom = "﻿"; // force l'encodage UTF-8 dans Excel
        const blob = new Blob([bom + (text ?? "")], { type });
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = filename || "export.csv";
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        // Libère l'URL après un court délai pour laisser le téléchargement démarrer.
        setTimeout(() => URL.revokeObjectURL(url), 1500);
    } catch (e) {
        console.error("hbaDownload a échoué :", e);
    }
};
