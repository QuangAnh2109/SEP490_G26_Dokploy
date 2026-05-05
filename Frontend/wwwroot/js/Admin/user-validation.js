const AdminUserValidation = {
    isValidEmail: function(email) {
        return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
    },
    isValidFullName: function(name) {
        return name && name.trim().length >= 2 && name.trim().length <= 200;
    },
    isValidStudentId: function(id) {
        return /^[A-Za-z]{2}\d{6}$/.test((id || '').trim());
    },
    isValidPhoneNumber: function(phone) {
        return /^0\d{9}$/.test((phone || '').trim());
    }
};
