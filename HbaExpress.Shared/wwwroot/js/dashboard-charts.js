// ============================================================
// HbaMarket Pro — rendu des graphiques du tableau de bord (Chart.js v4)
// Exposé via window.hbaCharts pour être appelé depuis Blazor (IJSRuntime).
// ============================================================
(function () {
    // Conserve une instance de chart par identifiant de canvas afin de
    // pouvoir la détruire proprement avant d'en recréer une (évite les fuites
    // et le "canvas already in use" au re-rendu Blazor).
    const charts = {};

    function renderSalesLine(canvasId, labels, data) {
        if (typeof Chart === "undefined") return; // Chart.js pas encore chargé
        const canvas = document.getElementById(canvasId);
        if (!canvas) return; // le <canvas> n'est pas (encore) dans le DOM

        // Détruit un éventuel graphique précédent sur ce canvas.
        if (charts[canvasId]) {
            charts[canvasId].destroy();
            delete charts[canvasId];
        }

        const ctx = canvas.getContext("2d");

        // Dégradé vert léger pour le remplissage sous la courbe.
        const gradient = ctx.createLinearGradient(0, 0, 0, canvas.height || 240);
        gradient.addColorStop(0, "rgba(31, 138, 76, 0.22)");
        gradient.addColorStop(1, "rgba(31, 138, 76, 0.00)");

        charts[canvasId] = new Chart(ctx, {
            type: "line",
            data: {
                labels: labels || [],
                datasets: [{
                    data: data || [],
                    borderColor: "#1F8A4C",
                    backgroundColor: gradient,
                    fill: true,
                    tension: 0.4,
                    borderWidth: 2.5,
                    pointRadius: 3,
                    pointBackgroundColor: "#1F8A4C",
                    pointBorderColor: "#FFFFFF",
                    pointBorderWidth: 1.5,
                    pointHoverRadius: 5
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        backgroundColor: "#0F5C30",
                        padding: 10,
                        cornerRadius: 8,
                        displayColors: false,
                        callbacks: {
                            label: function (item) {
                                const v = item.parsed.y || 0;
                                return v.toLocaleString("fr-FR") + " XOF";
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        grid: { display: false },
                        border: { display: false },
                        ticks: { color: "#888780", font: { size: 12 } }
                    },
                    y: {
                        beginAtZero: true,
                        grid: { color: "rgba(229, 226, 216, 0.6)" },
                        border: { display: false },
                        ticks: {
                            color: "#888780",
                            font: { size: 12 },
                            maxTicksLimit: 4,
                            callback: function (value) {
                                if (value >= 1000) return (value / 1000) + "k";
                                return value;
                            }
                        }
                    }
                }
            }
        });
    }

    window.hbaCharts = { renderSalesLine: renderSalesLine };
})();
