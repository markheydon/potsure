window.potsurePasskeyStepUp = {
  async performPasskeyStepUp(requestVerificationToken) {
    const headers = {
      "Content-Type": "application/json",
      RequestVerificationToken: requestVerificationToken,
    };

    const optionsResponse = await fetch("/identity/reauth/passkey/options", {
      method: "POST",
      credentials: "include",
      headers,
    });

    if (!optionsResponse.ok) {
      return { ok: false, message: "Could not start passkey verification." };
    }

    const optionsJson = await optionsResponse.json();
    const credential = await navigator.credentials.get({
      publicKey: PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson),
    });

    if (!credential) {
      return { ok: false, message: "Passkey verification was cancelled." };
    }

    const verifyResponse = await fetch("/identity/reauth/passkey/verify", {
      method: "POST",
      credentials: "include",
      headers,
      body: JSON.stringify({ credentialJson: JSON.stringify(credential) }),
    });

    if (!verifyResponse.ok) {
      return { ok: false, message: "Passkey verification failed." };
    }

    return { ok: true };
  },
};
