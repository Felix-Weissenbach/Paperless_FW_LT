async function loadDocuments() {
    const listDiv = document.getElementById("documentsList");
    listDiv.textContent = "Loading documents...";

    try {
        const response = await fetch("/document"); // GET request
        if (!response.ok) {
            listDiv.textContent = `Failed to load: ${response.status}`;
            return;
        }

        const documents = await response.json();

        if (documents.length === 0) {
            listDiv.textContent = "No documents found.";
            return;
        }

        // Create a table or list
        const table = document.createElement("table");
        table.className = "document-table";

        // Create header
        const header = table.insertRow();
        ["ID", "File Name", "Size (bytes)", "Uploaded At"].forEach(text => {
            const th = document.createElement("th");
            th.textContent = text;
            header.appendChild(th);
        });

        // Add rows
        documents.forEach(doc => {
            const row = table.insertRow();
            [doc.id, doc.fileName, doc.fileSize, new Date(doc.uploadedAt).toLocaleString()].forEach(text => {
                const cell = row.insertCell();
                cell.textContent = text;
            });
        });

        listDiv.innerHTML = ""; // clear previous content
        listDiv.appendChild(table);
    } catch (err) {
        listDiv.textContent = `Error: ${err.message}`;
    }
}

// Load documents on page load
document.addEventListener("DOMContentLoaded", loadDocuments);

// Also reload after a successful upload
document.getElementById("uploadForm").addEventListener("submit", async (event) => {
    event.preventDefault();

    const fileInput = document.getElementById("fileInput");
    const statusDiv = document.getElementById("status");

    if (fileInput.files.length === 0) {
        statusDiv.textContent = "Please select a file first.";
        return;
    }

    const file = fileInput.files[0];

    const formData = new FormData();
    formData.append("file", file);

    formData.append("fileName", file.name);
    //formData.append("storagePath", "");
    formData.append("fileSize", file.size);
    formData.append("uploadedAt", new Date().toISOString());

    statusDiv.textContent = `Uploading metadata for "${file.name}"...`;

    try {
        const response = await fetch("/document", {
            method: "POST",
            body: formData
        });

        if (response.ok) {
            const result = await response.json();
            statusDiv.textContent = `Document uploaded successfully! ID: ${result.id}`;

            // Reload the table after successful upload
            loadDocuments();
        } else {
            const errorText = await response.text();
            statusDiv.textContent = `Upload failed: ${response.status} - ${errorText}`;
        }
    } catch (err) {
        statusDiv.textContent = `Error: ${err.message}`;
    }
});

document.getElementById("searchBtn").addEventListener("click", async () => {
    const q = document.getElementById("searchInput").value;
    const resultDiv = document.getElementById("searchResults");

    if (!q) {
        resultDiv.textContent = "Please enter a search term.";
        return;
    }

    resultDiv.textContent = "Searching...";

    try {
        const res = await fetch(`/search?q=${encodeURIComponent(q)}`);
        if (!res.ok) {
            resultDiv.textContent = "Search failed.";
            return;
        }

        const results = await res.json();

        if (results.length === 0) {
            resultDiv.textContent = "No documents found.";
            return;
        }

        const ul = document.createElement("ul");
        results.forEach(doc => {
            const li = document.createElement("li");
            li.textContent = `${doc.fileName} (${doc.id})`;
            ul.appendChild(li);
        });

        resultDiv.innerHTML = "";
        resultDiv.appendChild(ul);
    } catch (err) {
        resultDiv.textContent = "Error: " + err.message;
    }
});


const toggleBtn = document.getElementById("toggleViewBtn");
const uploadView = document.getElementById("uploadView");
const searchView = document.getElementById("searchView");

let showingSearch = false;

toggleBtn.addEventListener("click", () => {
    showingSearch = !showingSearch;

    uploadView.style.display = showingSearch ? "none" : "block";
    searchView.style.display = showingSearch ? "block" : "none";

    toggleBtn.textContent = showingSearch ? "Upload" : "Search";
});
