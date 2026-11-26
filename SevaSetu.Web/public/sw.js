self.addEventListener("push", (event) => {
  const data = event.data ? event.data.json() : { title: "AaoSaharaBane", body: "A care update is waiting." };
  event.waitUntil(self.registration.showNotification(data.title || "AaoSaharaBane", { body: data.body || "" }));
});
