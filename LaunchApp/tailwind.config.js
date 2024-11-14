const lumexui = require("C:/Users/Andrey/.nuget/packages/lumexui/1.0.0-preview.3/theme/plugin");

/** @type {import("tailwindcss").Config} */
module.exports = {
    content: [
        "./**/*.{razor,html,cshtml}",
        "C:/Users/Andrey/.nuget/packages/lumexui/1.0.0-preview.3/theme/components/*.cs"
    ],
    theme: {
        extend: {},
    },
    plugins: [lumexui],
};
