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
    statusDiv.textContent = `Uploading metadata for "${file.name}"...`;

    const documentDTO = {
        fileName: file.name,
        storagePath: "",
        fileSize: file.size,
        uploadedAt: new Date().toISOString()
    };

    try {
        const response = await fetch("/document", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(documentDTO)
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