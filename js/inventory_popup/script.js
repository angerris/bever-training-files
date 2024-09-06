window.onload = async () => {
  const queryParams = new URLSearchParams(window.location.search);
  const data = queryParams.get("data");
  let inventoryId = null;

  if (data) {
    const parsedData = JSON.parse(decodeURIComponent(data));
    inventoryId = parsedData.inventoryId;
  }

  const fetchXml = `
      <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
        <entity name="cr8c9_product">
          <attribute name="cr8c9_productid"/>
          <attribute name="cr8c9_name"/>
          <order attribute="cr8c9_name" descending="false"/>
        </entity>
      </fetch>
    `;

  const products = await parent.Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_product",
    `?fetchXml=${encodeURIComponent(fetchXml)}`
  );

  const productSelect = document.getElementById("productSelect");

  products.entities.forEach((element) => {
    const option = document.createElement("option");
    option.value = element["cr8c9_productid"];
    option.textContent = element["cr8c9_name"];
    productSelect.appendChild(option);
  });

  document.getElementById("okButton").addEventListener("click", async () => {
    const selectedProductId = productSelect.value;
    const selectedOption = document.getElementById("inOutSelect").value;
    const quantity = parseInt(document.getElementById("quantity").value, 10);
    const productName = productSelect.options[productSelect.selectedIndex].text;

    const inventoryProductFetchXml = `
      <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
        <entity name="cr8c9_inventory_product">
          <attribute name="cr8c9_inventory_productid"/>
          <attribute name="cr8c9_fk_product_"/>
          <attribute name="cr8c9_int_quantity"/> 
          <filter type="and">
            <condition attribute="cr8c9_fk_inventory" operator="eq" value="${inventoryId}"/>
          </filter>
        </entity>
      </fetch>
    `;

    const inventoryProducts = await parent.Xrm.WebApi.retrieveMultipleRecords(
      "cr8c9_inventory_product",
      `?fetchXml=${encodeURIComponent(inventoryProductFetchXml)}`
    );

    const productEntry = inventoryProducts.entities.find(
      (product) => product["_cr8c9_fk_product__value"] === selectedProductId
    );

    if (selectedOption === "in") {
      if (productEntry) {
        await parent.Xrm.WebApi.updateRecord(
          "cr8c9_inventory_product",
          productEntry["cr8c9_inventory_productid"],
          {
            cr8c9_int_quantity:
              (productEntry["cr8c9_int_quantity"] || 0) + quantity
          }
        );
      } else {
        await parent.Xrm.WebApi.createRecord("cr8c9_inventory_product", {
          "cr8c9_fk_product_@odata.bind": `/cr8c9_products(${selectedProductId})`,
          "cr8c9_fk_inventory@odata.bind": `/cr8c9_inventories(${inventoryId.replace(
            /[{}]/g,
            ""
          )})`,
          cr8c9_int_quantity: quantity,
          cr8c9_name: productName,
          cr8c9_mon_price_per_unit: 1,
          cr8c9_mon_total_amount: quantity
        });
      }
    } else if (selectedOption === "out") {
      if (
        productEntry &&
        (productEntry["cr8c9_int_quantity"] || 0) >= quantity
      ) {
        await parent.Xrm.WebApi.updateRecord(
          "cr8c9_inventory_product",
          productEntry["cr8c9_inventory_productid"],
          {
            cr8c9_int_quantity:
              (productEntry["cr8c9_int_quantity"] || 0) - quantity
          }
        );
      } else {
        alert("Not enough products in the inventory.");
      }
    }

    parent.location.reload();
    window.close();
  });

  document.getElementById("cancelButton").addEventListener("click", () => {
    window.close();
  });
};
