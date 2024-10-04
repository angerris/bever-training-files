window.onload = async () => {
  const queryParams = new URLSearchParams(window.location.search);
  const data = queryParams.get("data");
  let inventoryId = null;

  if (data) {
    const parsedData = JSON.parse(decodeURIComponent(data));
    inventoryId = parsedData.inventoryId;
  }

  const products = await fetchProducts();
  populateProductSelect(products);

  document.getElementById("okButton").addEventListener("click", async () => {
    const selectedProductId = document.getElementById("productSelect").value;
    const selectedOption = document.getElementById("inOutSelect").value;
    const quantity = parseInt(document.getElementById("quantity").value, 10);
    const productName = getSelectedProductName();

    const inventoryProducts = await fetchInventoryProducts(inventoryId);

    await handleProductEntry(
      selectedOption,
      selectedProductId,
      quantity,
      productName,
      inventoryProducts,
      inventoryId
    );

    parent.location.reload();
    window.close();
  });

  document.getElementById("cancelButton").addEventListener("click", () => {
    window.close();
  });
};

const fetchProducts = async () => {
  const fetchXml = `
    <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
      <entity name="cr8c9_product">
        <attribute name="cr8c9_productid"/>
        <attribute name="cr8c9_name"/>
        <order attribute="cr8c9_name" descending="false"/>
      </entity>
    </fetch>
  `;

  return await parent.Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_product",
    `?fetchXml=${encodeURIComponent(fetchXml)}`
  );
};

const populateProductSelect = (products) => {
  const productSelect = document.getElementById("productSelect");

  products.entities.forEach((element) => {
    const option = document.createElement("option");
    option.value = element["cr8c9_productid"];
    option.textContent = element["cr8c9_name"];
    productSelect.appendChild(option);
  });
};

const fetchInventoryProducts = async (inventoryId) => {
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

  return await parent.Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_inventory_product",
    `?fetchXml=${encodeURIComponent(inventoryProductFetchXml)}`
  );
};

const handleProductEntry = async (
  selectedOption,
  selectedProductId,
  quantity,
  productName,
  inventoryProducts,
  inventoryId
) => {
  const productEntry = inventoryProducts.entities.find(
    (product) => product["_cr8c9_fk_product__value"] === selectedProductId
  );

  if (selectedOption === "in") {
    await handleProductEntryIn(
      productEntry,
      selectedProductId,
      quantity,
      productName,
      inventoryId
    );
  } else if (selectedOption === "out") {
    await handleProductEntryOut(productEntry, quantity);
  }
};

const handleProductEntryIn = async (
  productEntry,
  selectedProductId,
  quantity,
  productName,
  inventoryId
) => {
  if (productEntry) {
    await parent.Xrm.WebApi.updateRecord(
      "cr8c9_inventory_product",
      productEntry["cr8c9_inventory_productid"],
      {
        cr8c9_int_quantity: (productEntry["cr8c9_int_quantity"] || 0) + quantity
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
};

const handleProductEntryOut = async (productEntry, quantity) => {
  if (productEntry) {
    const currentQuantity = productEntry["cr8c9_int_quantity"] || 0;

    if (currentQuantity >= quantity) {
      const newQuantity = currentQuantity - quantity;
      if (newQuantity > 0) {
        await parent.Xrm.WebApi.updateRecord(
          "cr8c9_inventory_product",
          productEntry["cr8c9_inventory_productid"],
          {
            cr8c9_int_quantity: newQuantity
          }
        );
      } else {
        await parent.Xrm.WebApi.deleteRecord(
          "cr8c9_inventory_product",
          productEntry["cr8c9_inventory_productid"]
        );
      }
    } else {
      alert("Not enough products in the inventory.");
    }
  }
};

const getSelectedProductName = () => {
  const productSelect = document.getElementById("productSelect");
  return productSelect.options[productSelect.selectedIndex].text;
};
