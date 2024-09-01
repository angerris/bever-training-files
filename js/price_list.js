async function pricelistInit(formContext) {
  const priceListId = formContext.data.entity
    .getId()
    .replace("{", "")
    .replace("}", "");
  const currency = formContext
    .getAttribute("transactioncurrencyid")
    .getValue()[0]
    .id.replace("{", "")
    .replace("}", "");

  const getPlural = async (entity) => {
    const metadata = await Xrm.Utility.getEntityMetadata(entity, "");
    return metadata.EntitySetName;
  };

  const [priceListPlural, productPlural, currencyPlural] = await Promise.all([
    getPlural("cr8c9_price_list"),
    getPlural("cr8c9_product"),
    getPlural("transactioncurrency")
  ]);

  const fetchXML = `
      <fetch>
          <entity name="cr8c9_price_list_item">
              <attribute name="cr8c9_price_list_itemid" />
              <filter>
                  <condition attribute="cr8c9_fk_price_list" operator="eq" value="${priceListId}" />
              </filter>
          </entity>
      </fetch>`;

  const priceListItems = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_price_list_item",
    `?fetchXml=${encodeURIComponent(fetchXML)}`
  );
  for (let i = 0; i < priceListItems.entities.length; i++) {
    const priceListItemId = priceListItems.entities[i].cr8c9_price_list_itemid;
    await Xrm.WebApi.deleteRecord("cr8c9_price_list_item", priceListItemId);
  }
  const products = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_product",
    "?$select=cr8c9_productid,cr8c9_name"
  );
  for (let i = 0; i < products.entities.length; i++) {
    const product = products.entities[i];
    const newPriceListItem = {
      "cr8c9_fk_price_list@odata.bind": `/${priceListPlural}(${priceListId})`,
      "cr8c9_fk_product@odata.bind": `/${productPlural}(${product.cr8c9_productid})`,
      "transactioncurrencyid@odata.bind": `/${currencyPlural}(${currency})`,
      cr8c9_mon_price: 1,
      cr8c9_name: product.cr8c9_name
    };
    await Xrm.WebApi.createRecord("cr8c9_price_list_item", newPriceListItem);
  }

  formContext.data.refresh();
}
