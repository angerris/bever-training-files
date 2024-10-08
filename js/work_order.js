//------------filter contacts start------------
let contactPointer = null;

async function filterContacts(executionContext) {
  const formContext = executionContext.getFormContext();
  const customerLookup = formContext
    .getAttribute("cr8c9_fk_customer")
    .getValue();
  if (customerLookup) {
    const customerId = customerLookup[0].id.replace(/[{}]/g, "");
    const contactIds = await getContactIds(customerId);
    if (contactPointer !== null) {
      formContext
        .getControl("cr8c9_fk_contact")
        .removePreSearch(contactPointer);
    }
    if (contactIds) {
      contactPointer = function () {
        addCustomFilter(formContext, contactIds);
      };
    } else {
      contactPointer = function () {
        applyEmptyFilter(formContext);
      };
    }
    formContext.getControl("cr8c9_fk_contact").addPreSearch(contactPointer);
  }
}

async function getContactIds(customerId) {
  const fetchXml = `
    <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="true">
      <entity name="cr8c9_contact">
        <attribute name="cr8c9_contactid" />
        <attribute name="cr8c9_name" />
        <order attribute="cr8c9_name" descending="false" />
        <link-entity name="cr8c9_position" from="cr8c9_fk_contact" to="cr8c9_contactid" link-type="inner" alias="ae">
          <link-entity name="cr8c9_account" from="cr8c9_accountid" to="cr8c9_fk_account" link-type="inner" alias="af">
            <filter type="and">
              <condition attribute="cr8c9_accountid" operator="eq" value="${customerId}" />
            </filter>
          </link-entity>
        </link-entity>
      </entity>
    </fetch>
  `;
  const encodedFetchXml = encodeURIComponent(fetchXml);
  const result = await Xrm.WebApi.retrieveMultipleRecords(
    "cr8c9_contact",
    `?fetchXml=${encodedFetchXml}`
  );
  return result.entities.map((contact) => contact.cr8c9_contactid);
}

function addCustomFilter(formContext, contactIds) {
  const filterXml = `
    <filter type="and">
      <condition attribute="cr8c9_contactid" operator="in">
        ${contactIds.map((id) => `<value>${id}</value>`).join("")}
      </condition>
    </filter>
  `;
  formContext
    .getControl("cr8c9_fk_contact")
    .addCustomFilter(filterXml, "cr8c9_contact");
}

function applyEmptyFilter(formContext) {
  const emptyFilterXml = `
    <filter type="and">
      <condition attribute="cr8c9_contactid" operator="null" />
    </filter>
  `;
  formContext
    .getControl("cr8c9_fk_contact")
    .addCustomFilter(emptyFilterXml, "cr8c9_contact");
}
//------------filter contacts end------------

//------------update work order totals start------------
async function updateWorkOrderTotals(formContext) {
  const workOrderId = formContext.data.entity.getId().replace(/[{}]/g, "");
  const totalProductsAmount = await getSumOfField(
    "cr8c9_work_order_product",
    "cr8c9_mon_total_amount",
    "cr8c9_fk_work_order",
    workOrderId
  );
  const totalServicesAmount = await getSumOfField(
    "cr8c9_work_order_service",
    "cr8c9_mon_total_amount",
    "cr8c9_fk_work_order",
    workOrderId
  );

  formContext
    .getAttribute("cr8c9_mon_total_products_amount")
    .setValue(totalProductsAmount);
  formContext
    .getAttribute("cr8c9_mon_total_services_amount")
    .setValue(totalServicesAmount);
}

async function getSumOfField(
  entityLogicalName,
  fieldName,
  lookupFieldName,
  lookupId
) {
  const fetchXml = `
        <fetch aggregate="true" version="1.0">
            <entity name="${entityLogicalName}">
                <attribute name="${fieldName}" aggregate="sum" alias="totalSum" />
                <filter type="and">
                    <condition attribute="${lookupFieldName}" operator="eq" value="${lookupId}" />
                </filter>
            </entity>
        </fetch>
    `;

  const response = await Xrm.WebApi.retrieveMultipleRecords(
    entityLogicalName,
    `?fetchXml=${encodeURIComponent(fetchXml)}`
  );
  const totalSum = response.entities.length
    ? response.entities[0]["totalSum"]
    : 0;
  return totalSum;
}
//------------update work order totals end------------

function disableFieldsOnClosed(executionContext) {
  const formContext = executionContext.getFormContext();
  const closedStatus = 976090001;
  if (formContext.getAttribute("cr8c9_os_status").getValue() === closedStatus) {
    formContext.ui.controls.forEach((control) => {
      if (control && control.getName() !== "cr8c9_os_status") {
        control.setDisabled(true);
      }
    });
  }
}
