var dataTable;
$(document).ready(function () {
    LoadDataTable();
});

function LoadDataTable() {
    dataTable = $('#tblData').DataTable({
        "ajax": { url: '/admin/user/getall' },
        "columns": [       
            { data: 'name',"width":"20%" },
            { data: 'email', "width": "15%" },
            { data: 'phoneNumber', "width": "15%" },
            { data: 'company.companyName', "width": "10%" },
            { data: 'role', "width": "10%" },
            {
                data: { id: "id", lockoutEnd: "lockoutEnd" },               
                "render": function (data) {
                    var today = new Date().getTime();
                    var lockEnd = new Date(data.lockoutEnd).getTime();
                    if (lockEnd > today) {
                        return `
                        <div class="text-center">
                           <a onclick="LockUnlock('${data.id}')" class="btn btn-danger text-white" style="cursor:pointer;width:120px">
                               <i class="bi bi-lock-fill"></i>
						     Locked
                           </a>
                           <a href="/Admin/user/RoleManagement?userId=${data.id}" class="btn btn-danger text-white" style="cursor:pointer;width:130px">
                               <i class="bi bi-pencil-square"></i>
						     Permission
                           </a>
                       </div>
                       `
                    }
                    else {
                        return `
                        <div class="text-center">
                           <a onclick="LockUnlock('${data.id}')" class="btn btn-success text-white" style="cursor:pointer;width:120px">
                               <i class="bi bi-lock-fill"></i>
						     Unlocked
                           </a>
                           <a href="/Admin/user/RoleManagement?userId=${data.id}" class="btn btn-danger text-white" style="cursor:pointer;width:130px">
                               <i class="bi bi-pencil-square"></i>
						     Permission
                           </a>
                       </div>
                       `
                    }
                    
                },
                "width": "30%" 
            }
        ]
    });

}
function LockUnlock(id) {
    $.ajax({
        type: "POST",
        url: '/admin/user/LockUnlock',
        data: JSON.stringify(id),
        contentType: "application/json",
        success: function (data) {
            if (data.success) {
                toastr.success(data.message);
                dataTable.ajax.reload();
            }
            else {
                toastr.error(data.message);
            }
        }
    });
}   
