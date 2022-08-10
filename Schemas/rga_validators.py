from yamale.validators import Validator
import validators

class License(Validator):
    tag = "license"
    licenses = [
        "CC-BY-SA-3.0",
        "CC-BY-SA-4.0",
        "CC-BY-NC-3.0",
        "CC-BY-NC-4.0",
        "CC-BY-NC-SA-3.0",
        "CC-BY-NC-SA-4.0",
        "CC0-1.0",
        "MIT"
        ]

    def _is_valid(self, value):
        return value in self.licenses

class Url(Validator):
    tag = "url"

    def _is_valid(self, value):
        return validators.url(value)