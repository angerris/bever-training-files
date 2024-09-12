let productPointer = null;

async function filterProducts(executionContext) {
  const formContext = executionContext.getFormContext();
  const inventoryLookup = formContext
    .getAttribute("cr8c9_fk_inventory")
    .getValue();

  if (inventoryLookup && inventoryLookup.length > 0) {
    const inventoryId = inventoryLookup[0].id.replace(/[{}]/g, "");
    const fetchXml = `
      <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false">
        <entity name="cr8c9_inventory_product">
          <attribute name="cr8c9_inventory_productid" />
          <attribute name="cr8c9_name" />
          <order attribute="cr8c9_name" descending="false" />
          <filter type="and">
            <condition attribute="cr8c9_fk_inventory" operator="eq" value="${inventoryId}" />
          </filter>
          <link-entity name="cr8c9_product" from="cr8c9_productid" to="cr8c9_fk_product_" link-type="inner" alias="an">
            <attribute name="cr8c9_productid" />
            <filter type="and">
              <condition attribute="cr8c9_os_type" operator="eq" value="976090000" />
            </filter>
          </link-entity>
        </entity>
      </fetch>
    `;

    const encodedFetchXml = encodeURIComponent(fetchXml);

    const result = await Xrm.WebApi.retrieveMultipleRecords(
      "cr8c9_inventory_product",
      `?fetchXml=${encodedFetchXml}`
    );

    if (productPointer !== null) {
      formContext
        .getControl("cr8c9_fk_product")
        .removePreSearch(productPointer);
    }

    if (result.entities.length > 0) {
      const productIds = result.entities.map(
        (inventoryProduct) => inventoryProduct["an.cr8c9_productid"]
      );
      productPointer = function () {
        addProductFilter(formContext, productIds);
      };
      formContext.getControl("cr8c9_fk_product").addPreSearch(productPointer);
    } else {
      productPointer = function () {
        applyEmptyProductFilter(formContext);
      };
      formContext.getControl("cr8c9_fk_product").addPreSearch(productPointer);
    }
  }
}

function addProductFilter(formContext, productIds) {
  const filterXml = `
    <filter type="and">
      <condition attribute="cr8c9_productid" operator="in">
        ${productIds.map((id) => `<value>${id}</value>`).join("")}
      </condition>
    </filter>
  `;
  formContext
    .getControl("cr8c9_fk_product")
    .addCustomFilter(filterXml, "cr8c9_product");
}

function applyEmptyProductFilter(formContext) {
  const emptyFilterXml = `
    <filter type="and">
      <condition attribute="cr8c9_productid" operator="null" />
    </filter>
  `;
  formContext
    .getControl("cr8c9_fk_product")
    .addCustomFilter(emptyFilterXml, "cr8c9_product");
}
