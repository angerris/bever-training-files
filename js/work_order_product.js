//------------update inventory start------------
async function updateInventory(executionContext) {
  const formContext = executionContext.getFormContext();
  const product = formContext.getAttribute("cr8c9_fk_product").getValue();

  if (!product) {
    return [];
  }
  const productId = product[0].id.replace(/[{}]/g, "");
  const maxQuantity = await fetchMaxQuantity(productId);
  if (!maxQuantity) {
    return [];
  }
  const inventory = await fetchInventory(productId, maxQuantity);
  if (!inventory) {
    return [];
  }
  setInventory(formContext, inventory);
  return inventory;
}

async function fetchMaxQuantity(productId) {
  const fetchXmlMaxQuantity = `
    <fetch aggregate="true" version="1.0">
      <entity name="cr8c9_inventory_product">
        <attribute name="cr8c9_int_quantity" alias="maxQuantity" aggregate="max" />
        <filter type="and">
          <condition attribute="cr8c9_fk_product_" operator="eq" value="${productId}" />
        </filter>
      </entity>
    </fetch>`;

  const result = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_inventory_product",
    `?fetchXml=${encodeURIComponent(fetchXmlMaxQuantity)}`
  );

  return result.entities.length ? result.entities[0]["maxQuantity"] : null;
}

async function fetchInventory(productId, maxQuantity) {
  const fetchXmlInventory = `
    <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">
      <entity name="cr8c9_inventory">
        <attribute name="cr8c9_inventoryid"/>
        <attribute name="cr8c9_name"/>
        <attribute name="createdon"/>
        <order attribute="cr8c9_name" descending="false"/>
        <link-entity name="cr8c9_inventory_product" from="cr8c9_fk_inventory" to="cr8c9_inventoryid" link-type="inner" alias="ab">
          <filter type="and">
            <condition attribute="cr8c9_int_quantity" operator="eq" value="${maxQuantity}"/>
            <condition attribute="cr8c9_fk_product_" operator="eq" value="${productId}"/>
          </filter>
        </link-entity>
      </entity>
    </fetch>`;

  const result = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_inventory",
    `?fetchXml=${encodeURIComponent(fetchXmlInventory)}`
  );

  return result.entities.length ? result.entities[0] : null;
}

function setInventory(formContext, inventory) {
  const inventoryId = inventory.cr8c9_inventoryid;
  const inventoryName = inventory.cr8c9_name;

  formContext.getAttribute("cr8c9_fk_inventory").setValue([
    {
      id: inventoryId,
      entityType: "cr8c9_inventory",
      name: inventoryName
    }
  ]);
  formContext.getAttribute("cr8c9_fk_inventory").fireOnChange();
}
//------------update inventory end------------

//------------autofill price per unit start------------
async function autofillPricePerUnitInWorkOrderProduct(executionContext) {
  const formContext = executionContext.getFormContext();
  const inventoryField = formContext
    .getAttribute("cr8c9_fk_inventory")
    .getValue();
  const productField = formContext.getAttribute("cr8c9_fk_product").getValue();

  if (!inventoryField || !productField) {
    return;
  }
  const inventoryId = inventoryField[0].id.replace(/[{}]/g, "");
  const productId = productField[0].id.replace(/[{}]/g, "");
  const pricePerUnit = await fetchPricePerUnit(inventoryId, productId);
  formContext.getAttribute("cr8c9_mon_price_per_unit").setValue(pricePerUnit);
}

async function fetchPricePerUnit(inventoryId, productId) {
  const fetchXML = buildFetchXml(inventoryId, productId);
  const result = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_inventory",
    `?fetchXml=${encodeURIComponent(fetchXML)}`
  );
  if (result.entities.length) {
    return result.entities[0]["pli.cr8c9_mon_price"];
  }
  return await getDefaultPricePerUnit(productId);
}

function buildFetchXml(inventoryId, productId) {
  return `
    <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">
      <entity name="cr8c9_inventory">
        <attribute name="cr8c9_inventoryid" />
        <attribute name="cr8c9_fk_price_list" />
        <filter type="and">
          <condition attribute="cr8c9_inventoryid" operator="eq" value="${inventoryId}" />
        </filter>
        <link-entity name="cr8c9_price_list_item" from="cr8c9_fk_price_list" to="cr8c9_fk_price_list" link-type="inner" alias="pli">
          <attribute name="cr8c9_mon_price" />
          <filter type="and">
            <condition attribute="cr8c9_fk_product" operator="eq" value="${productId}" />
          </filter>
        </link-entity>
      </entity>
    </fetch>
  `;
}

async function getDefaultPricePerUnit(productId) {
  const productRecord = await Xrm.WebApi.retrieveRecord(
    "cr8c9_product",
    productId,
    "?$select=cr8c9_mon_price_per_unit"
  );
  return productRecord.cr8c9_mon_price_per_unit || 0;
}
//------------autofill price per unit end------------

//------------disable WO product field start------------
async function disableWOProductIfWorkOrderClosed(executionContext) {
  const formContext = executionContext.getFormContext();
  const workOrderId = formContext
    .getAttribute("cr8c9_fk_work_order")
    .getValue()?.[0]?.id;

  const closedStatus = 976090001;
  if (workOrderId && (await getWorkOrderStatus(workOrderId)) === closedStatus) {
    formContext.ui.controls.forEach((control) => control.setDisabled(true));
  }
}
async function getWorkOrderStatus(workOrderId) {
  const result = await Xrm.WebApi.retrieveRecord(
    "cr8c9_work_order",
    workOrderId.replace(/[{}]/g, ""),
    "?$select=cr8c9_os_status"
  );
  return result.cr8c9_os_status;
}
//------------disable WO product field end------------

//------------validate WO product quantity start------------
async function validateWorkOrderProductQuantity(executionContext) {
  const formContext = executionContext.getFormContext();
  const inventoryField = formContext.getAttribute("cr8c9_fk_inventory");
  const productField = formContext.getAttribute("cr8c9_fk_product");
  const quantityField = formContext.getAttribute("cr8c9_int_quantity");

  if (!productField || !quantityField || !inventoryField) return;
  const productId = productField.getValue();
  const inventoryId = inventoryField.getValue();
  const quantityToAdd = quantityField.getValue();

  if (!productId || !inventoryId || quantityToAdd === null) return;
  const availableQuantity = await getAvailableQuantityForProduct(
    productId[0].id.replace(/[{}]/g, ""),
    inventoryId[0].id.replace(/[{}]/g, "")
  );

  if (quantityToAdd > availableQuantity) {
    const errorMessage = `The maximum quantity you can add is ${availableQuantity}.`;
    quantityField.setIsValid(false);
    formContext.getControl("cr8c9_int_quantity").setNotification(errorMessage);
  } else {
    quantityField.setIsValid(true);
    formContext.getControl("cr8c9_int_quantity").clearNotification();
  }
}

async function getAvailableQuantityForProduct(productId, inventoryId) {
  const fetchXml = `
      <fetch top="1">
          <entity name="cr8c9_inventory_product">
              <attribute name="cr8c9_int_quantity" />
              <filter>
                  <condition attribute="cr8c9_fk_product_" operator="eq" value="${productId}" />
                  <condition attribute="cr8c9_fk_inventory" operator="eq" value="${inventoryId}" />
              </filter>
          </entity>
      </fetch>`;

  const results = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_inventory_product",
    `?fetchXml=${encodeURIComponent(fetchXml)}`
  );
  return results.entities.length ? results.entities[0].cr8c9_int_quantity : 0;
}
//------------validate WO product quantity end------------
function calculateTotalAmount(executionContext) {
  const formContext = executionContext.getFormContext();
  const pricePerUnit = formContext
    .getAttribute("cr8c9_mon_price_per_unit")
    .getValue();
  const quantity = formContext.getAttribute("cr8c9_int_quantity").getValue();
  const price = pricePerUnit ? parseFloat(pricePerUnit) : 0;
  const qty = quantity ? parseInt(quantity) : 0;
  const totalAmount = price * qty;
  formContext.getAttribute("cr8c9_mon_total_amount").setValue(totalAmount);
}
