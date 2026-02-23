import { useState } from "react";
import { api } from "./api";

export default function App() {
    const [message, setMessage] = useState<string>("");
    // Stavy pro oba textboxy
    const [inputEmail, setInputEmail] = useState<string>("");
    const [inputName, setInputName] = useState<string>("");
    const [inputMessage, setInputMessage] = useState<string>("");

    function addData() {
        api.post("/Contact/Store", {
            email: inputEmail,
            name: inputName,
            massage: inputMessage
        })
            .then((response) => {
                setMessage(response.data);
                setInputEmail("");
                setInputName("");
                setInputMessage("");
            })
            .catch(err => console.error(err));
    }

    return (
        <div style={{ padding: "20px", display: "flex", flexDirection: "column", gap: "10px", maxWidth: "300px" }}>
            <h1>Jsi gay</h1>
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
