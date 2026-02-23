import React, { useEffect, useState } from "react";
import { api } from "./api";

interface Contact {
    email: string;
    name: string;
    message: string;
}

export default function App() {
    const [message, setMessage] = useState<string>("");
    // Stavy pro oba textboxy
    const [inputEmail, setInputEmail] = useState<string>("");
    const [inputName, setInputName] = useState<string>("");
    const [inputMessage, setInputMessage] = useState<string>("");

    // store last submitted contact so we can show it in a paragraph
    const [lastContact, setLastContact] = useState<{ email: string; name: string; message: string } | null>(null);

    // full list of contacts shown in UI
    const [contacts, setContacts] = useState<Contact[]>([]);

    function addData() {
        api.post("/Contact/Store", {
            email: inputEmail,
            name: inputName,
            message: inputMessage
        })
            .then((response) => {
                setMessage(response.data);
                // save last submitted contact for display
                setLastContact({ email: inputEmail, name: inputName, message: inputMessage });
                // clear inputs
                setInputEmail("");
                setInputName("");
                setInputMessage("");

                // Optionally optimistically add to contacts so user sees it immediately
                setContacts(prev => [...prev, { email: inputEmail, name: inputName, message: inputMessage }]);
            })
            .catch(err => console.error(err));
    }

    useEffect(() => {
        let cancelled = false;
        let currentVersion = 0;

        // Fetch initial list
        (async () => {
            try {
                const resp = await api.get('/Contact/StoreAll');
                if (resp && resp.data) {
                    try {
                        const parsed = JSON.parse(resp.data) as Contact[];
                        setContacts(parsed);
                    } catch (err) {
                        // if the server already returned JSON (not string), handle it
                        setContacts(resp.data as Contact[]);
                    }
                }
            } catch (err) {
                console.error('Failed to load initial contacts', err);
            }
        })();

        async function poll() {
            while (!cancelled) {
                try {
                    const resp = await api.get('/Contact/WaitForChanges', { params: { sinceVersion: currentVersion, timeoutMs: 30000 } });
                    if (resp.status === 200 && resp.data) {
                        const data = resp.data;
                        // data.json is a JSON string of contacts
                        let parsed: Contact[] = [];
                        try {
                            parsed = JSON.parse(data.json);
                        } catch (err) {
                            // if already parsed
                            parsed = data.json as Contact[];
                        }

                        setContacts(parsed);
                        currentVersion = data.version ?? currentVersion;
                        setMessage(`Version ${currentVersion}`);
                    } else if (resp.status === 204) {
                        // no changes within timeout
                    }
                } catch (err) {
                    // network or cancellation - wait a bit before retry
                    await new Promise(r => setTimeout(r, 1000));
                }
            }
        }

        poll();

        return () => { cancelled = true; };
    }, []);

    return (
        <div style={{ padding: "20px", display: "flex", flexDirection: "column", gap: "10px", maxWidth: "600px" }}>
            <input
                value={inputName}
                onChange={(e) => setInputName(e.target.value)}
                placeholder="Name"
            />

            <input
                value={inputEmail}
                onChange={(e) => setInputEmail(e.target.value)}
                placeholder="Email"
            />

            <textarea
                value={inputMessage}
                onChange={(e) => setInputMessage(e.target.value)}
                placeholder="Message"
                style={{ height: "128px" }}
            />

            <button onClick={addData}>Send</button>

            <div style={{ marginTop: "20px", fontWeight: "bold" }}>
              {message}
            </div>

            {/* Paragraph showing the last submitted contact with all data */}
            {lastContact && (
                <p style={{ marginTop: 8 }}>
                    <strong>Submitted:</strong> {lastContact.name} — {lastContact.email} — {lastContact.message}
                </p>
            )}

            {/* Show full list of contacts */}
            <div style={{ marginTop: 12 }}>
                <h3>Contacts ({contacts.length})</h3>
                {contacts.length === 0 && <div>No contacts yet.</div>}
                {contacts.map((c, i) => (
                    <div key={i} style={{ padding: 8, borderBottom: '1px solid #ddd' }}>
                        <div style={{ fontWeight: 600 }}>{c.name} &lt;{c.email}&gt;</div>
                        <div style={{ marginTop: 4 }}>{c.message}</div>
                    </div>
                ))}
            </div>
        </div>
    );
}
