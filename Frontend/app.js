document.getElementById("createBtn").addEventListener("click", async () => {
  const statusDiv = document.getElementById("status");
  statusDiv.textContent = "Creating document...";

  try {
    const response = await fetch("/api/document", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        title: "Test Document from " + new Date().toISOString(),
        content: "This is a demo created from the frontend."
      }),
    });

    if (response.ok) {
      const result = await response.json();
      statusDiv.textContent = `Document created! ID: ${result.id}`;
    } else {
      const errorText = await response.text();
      statusDiv.textContent = `Failed: ${response.status} - ${errorText}`;
    }
  } catch (err) {
    statusDiv.textContent = `Error: ${err.message}`;
  }
});