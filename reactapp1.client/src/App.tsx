import { useEffect, useState } from "react";
import { api } from "./api";

export default function App() {
    const [message, setMessage] = useState<string>("");
    // Stavy pro oba textboxy
    const [inputEmail, setInputEmail] = useState<string>("");
    const [inputName, setInputName] = useState<string>("");
    const [inputMessage, setInputMessage] = useState<string>("");

    const [version, setVersion] = useState<number>(0);

    function addData() {
        api.post("/Contact/Store", {
            email: inputEmail,
            name: inputName,
            message: inputMessage
        })
            .then((response) => {
                setMessage(response.data);
                setInputEmail("");
                setInputName("");
                setInputMessage("");
            })
            .catch(err => console.error(err));
    }

    useEffect(() => {
        let cancelled = false;

        async function poll() {
            while (!cancelled) {
                try {
                    const resp = await api.get('/Contact/WaitForChanges', { params: { sinceVersion: version, timeoutMs: 30000 } });
                    if (resp.status === 200 && resp.data) {
                        const data = resp.data;
                        // data.json is a JSON string of contacts
                        setVersion(data.version ?? version);
                        setMessage(`Updated (v${data.version})`);
                        // Optionally parse and use the contacts: JSON.parse(data.json)
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
    }, [version]);

    return (
        <div style={{ padding: "20px", display: "flex", flexDirection: "column", gap: "10px", maxWidth: "300px" }}>
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

            <input
                value={inputMessage}
                onChange={(e) => setInputMessage(e.target.value)}
                placeholder="Message"
                style={{height: "256px"}}
            />

            <button onClick={addData}>Send</button>

            <div style={{ marginTop: "20px", fontWeight: "bold" }}>
              {message}
            </div>
        </div>
    );
}
